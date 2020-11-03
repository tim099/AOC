using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UCL.Core.Container;
using System.Net.Sockets;

namespace AOC.MapLib {
    public enum CubeSide {
        Null = 0,
        Right = 1 << 0,
        Left = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3,
        Front = 1 << 4,
        Back = 1 << 5
    }
    [UCL.Core.ATTR.EnableUCLEditor]
    public class AOC_CubeMesh : UCL.MeshLib.UCL_MeshCreator {
        class AtlasData {
            public AtlasData(float[] _Atlas) {
                m_Atlas = _Atlas;
            }
            public float[] m_Atlas;
        }
        static public ComponentPool<BoxCollider> m_ColliderPool = new ComponentPool<BoxCollider>();
        
        static UCL_Vector<AtlasData> m_AtlasVec = new UCL_Vector<AtlasData>(256);
        //static Dictionary<CubeSide, Vector3[]> VerticesDic = new Dictionary<CubeSide, Vector3[]>();
        public float m_CubeSize = 1.0f;
        public int m_UVSegment = 1;
        public Vector3Int m_GenSize = new Vector3Int(16, 16, 16);
        public Vector3Int m_TerrainSize;
        public Vector3Int m_TerrainStartPos;

        public UCL_Vector<Vector3> m_NormalVec = new UCL_Vector<Vector3>(4 * 128);
        public UCL_Vector<Vector3> m_VerticeVec = new UCL_Vector<Vector3>(4 * 128);
        public UCL_Vector<Vector2> m_UVsVec = new UCL_Vector<Vector2>(4 * 128);
        public UCL_Vector<int> m_TrianglesVec = new UCL_Vector<int>(3 * 128);

        public UCL.Core.TextureLib.UCL_TextureAtlasCreator m_TextureAtlasRuntime;

        //public Container.CL_Vector<ushort> m_Voxels = new Container.CL_Vector<ushort>();
        float m_UVSize = 1;//auto gen!!
        int[,,] m_Terrain;
        int m_FaceCount;
        public bool f_UpdateCollider = true;
        public bool f_MeshCollideOn = false;
        public bool f_Updating = false;
        void Reset() {
            Debug.LogWarning("Reset()");
        }
        [UCL.Core.ATTR.UCL_FunctionButton]
        public override void Init() {
            base.Init();
            var terrain = new int[m_GenSize.x, m_GenSize.y, m_GenSize.z];
            float sx = UCL.Core.MathLib.UCL_Random.Instance.NextFloat(100f);
            float sy = UCL.Core.MathLib.UCL_Random.Instance.NextFloat(100f);
            for(int i = 0; i < m_GenSize.x; i++) {
                for(int j = 0; j < m_GenSize.z; j++) {
                    float height = 0.5f*m_GenSize.y * UCL.Core.MathLib.Noise.PerlinNoiseUnsigned(sx+0.03f*j, sy+0.03f * i, 0);
                    if(height > m_GenSize.y) height = m_GenSize.y;
                    float val = UCL.Core.MathLib.Noise.PerlinNoiseUnsigned(sy + 0.05f * j, sx + 0.05f * i, 0);
                    int v = Mathf.RoundToInt(val * 8f);
                    if(v > 4) v = 4;
                    for(int k = 0; k < height; k++) {
                        terrain[i, k, j] = v;
                    }
                    //terrain[i, 0, j] = Random.Range(0,4)+1;
                }
            }
            GenTerrain(terrain, Vector3Int.zero);
        }

        public Vector3 GetSize() {
            return new Vector3(m_GenSize.x * m_CubeSize, m_GenSize.y * m_CubeSize, m_GenSize.z * m_CubeSize);
        }
        static public void AlterCubeSidePos(CubeSide side, ref Vector3Int pos) {
            switch(side) {
                case CubeSide.Right: pos.x += 1; break;
                case CubeSide.Left: pos.x -= 1; break;

                case CubeSide.Up: pos.y += 1; break;
                case CubeSide.Down: pos.y -= 1; break;

                case CubeSide.Front: pos.z += 1; break;
                case CubeSide.Back: pos.z -= 1; break;
            }
        }
        static Vector3Int s_RightV = new Vector3Int(1, 0, 0);
        static Vector3Int s_LeftV = new Vector3Int(-1, 0, 0);
        static Vector3Int s_UpV = new Vector3Int(0, 1, 0);
        static Vector3Int s_DownV = new Vector3Int(0, -1, 0);
        static Vector3Int s_FrontV = new Vector3Int(0, 0, 1);
        static Vector3Int s_BackV = new Vector3Int(0, 0, -1);
        static Vector3Int s_NoneV = new Vector3Int(0, 0, 0);

        static public Vector3Int GetCubeSidePos(CubeSide side) {

            switch(side) {
                case CubeSide.Right: return s_RightV;
                case CubeSide.Left: return s_LeftV;

                case CubeSide.Up: return s_UpV;
                case CubeSide.Down: return s_DownV;

                case CubeSide.Front: return s_FrontV;
                case CubeSide.Back: return s_BackV;
            }
            return s_NoneV;
        }
        public int GetTerrain(Vector3Int pos) {
            if(pos.x < 0 || pos.y < 0 || pos.z < 0 ||
                pos.x >= m_TerrainSize.x || pos.y >= m_TerrainSize.y || pos.z >= m_TerrainSize.z) return 0;
            return m_Terrain[pos.x, pos.y, pos.z];
        }
        public int GetTerrain(int x, int y, int z) {
            if(x < 0 || y < 0 || z < 0 || x >= m_TerrainSize.x || y >= m_TerrainSize.y || z >= m_TerrainSize.z) return 0;
            return m_Terrain[x, y, z];
        }
        void SetTerrain(int[,,] Terrain, Vector3Int StartPos) {
            m_Terrain = Terrain;
            m_TerrainSize = new Vector3Int(m_Terrain.GetLength(0), m_Terrain.GetLength(1), m_Terrain.GetLength(2));
            //m_Voxels.Resize(m_GenSize.x * m_GenSize.y * m_GenSize.z);

            m_TerrainStartPos = StartPos;
        }

        public List<KeyValuePair<Vector3, Vector3>> m_Boxes = new List<KeyValuePair<Vector3, Vector3>>();
        UCL_Vector<bool> m_Tested = new UCL_Vector<bool>();
        virtual public void GenMesh(int[,,] Terrain, Vector3Int StartPos) {
            m_UVSize = 1.0f / m_UVSegment;

            m_Boxes.Clear();
            m_FaceCount = 0;
            m_NormalVec.Clear();
            m_VerticeVec.Clear();
            m_UVsVec.Clear();
            m_TrianglesVec.Clear();

            int tested_len = m_GenSize.x * m_GenSize.y * m_GenSize.z;
            m_Tested.Resize(tested_len);

            int spx = StartPos.x, spy = StartPos.y, spz = StartPos.z;
            int px, py, pz;
            int pos = 0;
            for(int z = 0; z < m_GenSize.z; z++) {
                for(int y = 0; y < m_GenSize.y; y++) {
                    for(int x = 0; x < m_GenSize.x; x++) {
                        //Pos = StartPos + new Vector3Int(x, y, z);
                        px = spx + x;
                        py = spy + y;
                        pz = spz + z;
                        int type = m_Terrain[px, py, pz];
                        if(type > 0) {// Is cube!!
                            m_Tested.m_Arr[pos] = false;

                            type--;
                            CubeSide side_info = CubeSide.Null;
                            int side_count = 0;
                            if(px >= m_TerrainSize.x - 1 || m_Terrain[px + 1, py, pz] < 1) { side_info |= CubeSide.Right; side_count++; }
                            if(px <= 0 || m_Terrain[px - 1, py, pz] < 1) { side_info |= CubeSide.Left; side_count++; }
                            if(py >= m_TerrainSize.y - 1 || m_Terrain[px, py + 1, pz] < 1) { side_info |= CubeSide.Up; side_count++; }
                            if(py <= 0 || m_Terrain[px, py - 1, pz] < 1) { side_info |= CubeSide.Down; side_count++; }
                            if(pz >= m_TerrainSize.z - 1 || m_Terrain[px, py, pz + 1] < 1) { side_info |= CubeSide.Front; side_count++; }
                            if(pz <= 0 || m_Terrain[px, py, pz - 1] < 1) { side_info |= CubeSide.Back; side_count++; }

                            if(side_count > 0) AddAllSurface(side_info, x, y, z, type, side_count);
                        } else {
                            m_Tested.m_Arr[pos] = true;
                        }
                        pos++;
                    }
                }
            }
            pos = 0;
            #region GenCollider
            for(int z = 0; z < m_GenSize.z; z++) {
                for(int y = 0; y < m_GenSize.y; y++) {
                    for(int x = 0; x < m_GenSize.x; x++) {
                        if(!m_Tested.m_Arr[pos]) {//can gen!!
                            m_Tested.m_Arr[pos] = true;
                            bool sx = true, sy = true, sz = true;
                            int bsx = 1, bsy = 1, bsz = 1;
                            while(sx || sy || sz) {
                                if(sx) {
                                    int cx = x + bsx;
                                    if(cx < m_GenSize.x) {
                                        for(int i = 0; i < bsy; i++) {
                                            for(int j = 0; j < bsz; j++) {
                                                int yy = y + i, zz = z + j;
                                                if(m_Tested.m_Arr[cx + yy * m_GenSize.x + zz * m_GenSize.x * m_GenSize.y]) {//Already Tested!!
                                                    sx = false;
                                                    break;
                                                }
                                            }
                                        }
                                        if(sx) {
                                            for(int i = 0; i < bsy; i++) {
                                                for(int j = 0; j < bsz; j++) {
                                                    int yy = y + i, zz = z + j;
                                                    m_Tested.m_Arr[cx + yy * m_GenSize.x + zz * m_GenSize.x * m_GenSize.y] = true;
                                                }
                                            }
                                            bsx++;
                                        }
                                    } else {
                                        sx = false;
                                    }
                                }
                                if(sz) {
                                    int cz = z + bsz;
                                    if(cz < m_GenSize.z) {
                                        for(int i = 0; i < bsx; i++) {
                                            for(int j = 0; j < bsy; j++) {
                                                int xx = x + i, yy = y + j;
                                                if(m_Tested.m_Arr[xx + yy * m_GenSize.x + cz * m_GenSize.x * m_GenSize.y]) {//Already Tested!!
                                                    sz = false;
                                                    break;
                                                }
                                            }
                                        }
                                        if(sz) {
                                            for(int i = 0; i < bsx; i++) {
                                                for(int j = 0; j < bsy; j++) {
                                                    int xx = x + i, yy = y + j;
                                                    m_Tested.m_Arr[xx + yy * m_GenSize.x + cz * m_GenSize.x * m_GenSize.y] = true;
                                                }
                                            }
                                            bsz++;
                                        }
                                    } else {
                                        sz = false;
                                    }
                                }
                                if(sy) {
                                    int cy = y + bsy;
                                    if(cy < m_GenSize.y) {
                                        for(int i = 0; i < bsx; i++) {
                                            for(int j = 0; j < bsz; j++) {
                                                int xx = x + i, zz = z + j;
                                                if(m_Tested.m_Arr[xx + cy * m_GenSize.x + zz * m_GenSize.x * m_GenSize.y]) {//Already Tested!!
                                                    sy = false;
                                                    break;
                                                }
                                            }
                                        }
                                        if(sy) {
                                            for(int i = 0; i < bsx; i++) {
                                                for(int j = 0; j < bsz; j++) {
                                                    int xx = x + i, zz = z + j;
                                                    m_Tested.m_Arr[xx + cy * m_GenSize.x + zz * m_GenSize.x * m_GenSize.y] = true;
                                                }
                                            }
                                            bsy++;
                                        }
                                    } else {
                                        sy = false;
                                    }
                                }
                            }
                            var posc = new Vector3(m_CubeSize * (x + 0.5f * (bsx - 1)),
                                m_CubeSize * (y + 0.5f * (bsy - 1)), m_CubeSize * (z + 0.5f * (bsz - 1)));
                            var scale = new Vector3(bsx * m_CubeSize, bsy * m_CubeSize, bsz * m_CubeSize);

                            m_Boxes.Add(new KeyValuePair<Vector3, Vector3>(posc, scale));
                        }
                        pos++;
                    }
                }
            }
            #endregion
            GenTriangles(m_FaceCount);
        }
        virtual public void GenTerrainAsyc(int[,,] Terrain, Vector3Int StartPos, System.Action end_act = null) {
            if(m_TextureAtlasRuntime) {
                m_UVSegment = m_TextureAtlasRuntime.m_Seg;
            }

            SetTerrain(Terrain, StartPos);
            f_Updating = true;
            UCL.Core.ThreadLib.UCL_ThreadManager.Instance.Run(delegate () {
                GenMesh(Terrain, StartPos);
            }, delegate () {
                GenerateMesh();
                f_Updating = false;
                end_act?.Invoke();
            });
        }
        virtual public void GenTerrain(int[,,] Terrain, Vector3Int StartPos) {
            if(m_TextureAtlasRuntime) {
                m_UVSegment = m_TextureAtlasRuntime.m_Seg;
            }
            SetTerrain(Terrain, StartPos);
            GenMesh(Terrain, StartPos);

            GenerateMesh();
        }
        UCL_Vector<BoxCollider> m_BoxColliders = new UCL_Vector<BoxCollider>();
        virtual public void SetColliderActive(bool flag) {
            if(f_UpdateCollider == flag) return;

            f_UpdateCollider = flag;
            UpdateCollider();
        }
        public void UpdateCollider() {
            for(int i = 0, size = m_BoxColliders.Count; i < size; i++) {
                m_ColliderPool.Delete(m_BoxColliders[i]);
            }
            m_BoxColliders.Clear();
            if(f_UpdateCollider) {
                for(int i = 0, size = m_Boxes.Count; i < size; i++) {//can use a struct with list...
                    var Box = m_Boxes[i];

                    var BoxCollider = m_ColliderPool.Create(transform);
                    BoxCollider.transform.position = transform.position + Box.Key;
                    BoxCollider.transform.localScale = Box.Value;
                    m_BoxColliders.Add(BoxCollider);
                }
            }
        }
        override public void GenerateMesh() {
            UpdateCollider();

            var fil = GetComponent<MeshFilter>();
            if(fil == null) return;
            Mesh mesh = fil.sharedMesh;
            mesh.indexFormat = m_IndexFormat;

            mesh.Clear();
            mesh.SetVertices(m_VerticeVec.m_Arr);//new ArraySegment<Vector3>(m_VerticeVec.m_Arr,0, m_VerticeVec.Count).Array;
            mesh.SetNormals(m_NormalVec.m_Arr);
            mesh.SetTriangles(GenTriangles(m_FaceCount), 0, 6 * m_FaceCount, 0);
            mesh.uv = m_UVsVec.m_Arr;//m_UV.ToArray(); // add this line to the code here

            if(f_MeshCollideOn) {
                var mesh_collider = GetComponent<MeshCollider>();
                if(mesh_collider) mesh_collider.sharedMesh = mesh;
            }
        }
        void AddAllSurface(CubeSide side, int x, int y, int z, int type, int side_count) {
            const float c_size = 0.5f;
            int start_at = m_VerticeVec.Count;//m_Vertices.Count;
            const float UV_edge = 0.003f;

            m_UVsVec.AddCount(4 * side_count);
            m_VerticeVec.AddCount(4 * side_count);
            m_NormalVec.AddCount(4 * side_count);
            lock(m_AtlasVec) {
                int count = m_AtlasVec.Count;
                if(type >= count) {
                    m_AtlasVec.AddCount(type + 1 - m_AtlasVec.Count);
                    for(int i = count; i <= type; i++) {
                        float[] UV = new float[8];
                        Vector2Int UV_Pos = m_TextureAtlasRuntime.ConverPos(i);
                        UV[0] = (UV_edge + UV_Pos.x) * m_UVSize;
                        UV[1] = (UV_edge + UV_Pos.y) * m_UVSize;
                        UV[2] = (UV_Pos.x + 1 - UV_edge) * m_UVSize;
                        UV[3] = (UV_Pos.y + UV_edge) * m_UVSize;
                        UV[4] = (UV_Pos.x + 1 - UV_edge) * m_UVSize;
                        UV[5] = (UV_Pos.y + 1 - UV_edge) * m_UVSize;
                        UV[6] = (UV_edge + UV_Pos.x) * m_UVSize;
                        UV[7] = (UV_Pos.y + 1 - UV_edge) * m_UVSize;
                        var Data = new AtlasData(UV);
                        m_AtlasVec[i] = Data;
                    }
                }
            }

            var Atlas = m_AtlasVec[type].m_Atlas;

            for(int i = 0; i < side_count; i++) {
                m_UVsVec.m_Arr[start_at + i * 4].x = Atlas[0];
                m_UVsVec.m_Arr[start_at + i * 4].y = Atlas[1];

                m_UVsVec.m_Arr[start_at + 1 + i * 4].x = Atlas[2];
                m_UVsVec.m_Arr[start_at + 1 + i * 4].y = Atlas[3];

                m_UVsVec.m_Arr[start_at + 2 + i * 4].x = Atlas[4];
                m_UVsVec.m_Arr[start_at + 2 + i * 4].y = Atlas[5];

                m_UVsVec.m_Arr[start_at + 3 + i * 4].x = Atlas[6];
                m_UVsVec.m_Arr[start_at + 3 + i * 4].y = Atlas[7];
            }

            if((side & CubeSide.Left) != CubeSide.Null) {
                m_NormalVec.m_Arr[start_at].x = -1;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = -1;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = -1;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = -1;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;
            }
            if((side & CubeSide.Right) != CubeSide.Null) {
                m_NormalVec.m_Arr[start_at].x = 1;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 1;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 1;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 1;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;
            }
            if((side & CubeSide.Front) != CubeSide.Null) {
                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 1;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 1;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 1;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = 1;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

            }
            if((side & CubeSide.Back) != CubeSide.Null) {
                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = -1;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = -1;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = -1;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 0;
                m_NormalVec.m_Arr[start_at].z = -1;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;
            }
            if((side & CubeSide.Up) != CubeSide.Null) {
                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 1;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 1;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 1;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = 1;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;
            }
            if((side & CubeSide.Down) != CubeSide.Null) {
                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = -1;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = -1;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z + c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = -1;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;

                m_NormalVec.m_Arr[start_at].x = 0;
                m_NormalVec.m_Arr[start_at].y = -1;
                m_NormalVec.m_Arr[start_at].z = 0;

                m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                m_VerticeVec.m_Arr[start_at++].z = (z - c_size) * m_CubeSize;
            }

            m_FaceCount += side_count;
        }

        void AddSurface(CubeSide side, int x, int y, int z, Vector2Int UV_Pos) {
            const float c_size = 0.5f;
            int start_at = m_VerticeVec.Count;//m_Vertices.Count;
            const float UV_edge = 0.003f;
            m_UVsVec.AddCount(4);
            m_VerticeVec.AddCount(4);

            m_UVsVec.m_Arr[start_at].x = (UV_edge + UV_Pos.x) * m_UVSize;
            m_UVsVec.m_Arr[start_at].y = (UV_edge + UV_Pos.y) * m_UVSize;

            m_UVsVec.m_Arr[start_at + 1].x = (UV_Pos.x + 1 - UV_edge) * m_UVSize;
            m_UVsVec.m_Arr[start_at + 1].y = (UV_Pos.y + UV_edge) * m_UVSize;

            m_UVsVec.m_Arr[start_at + 2].x = (UV_Pos.x + 1 - UV_edge) * m_UVSize;
            m_UVsVec.m_Arr[start_at + 2].y = (UV_Pos.y + 1 - UV_edge) * m_UVSize;

            m_UVsVec.m_Arr[start_at + 3].x = (UV_edge + UV_Pos.x) * m_UVSize;
            m_UVsVec.m_Arr[start_at + 3].y = (UV_Pos.y + 1 - UV_edge) * m_UVSize;

            switch(side) {
                case CubeSide.Left: {
                        m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 1].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].z = (z - c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 2].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].z = (z - c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 3].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].z = (z + c_size) * m_CubeSize;

                        break;
                    }
                case CubeSide.Right: {
                        m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].z = (z - c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 1].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 2].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 3].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].z = (z - c_size) * m_CubeSize;
                        break;
                    }
                case CubeSide.Front: {
                        m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 1].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 2].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 3].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].z = (z + c_size) * m_CubeSize;
                        break;
                    }
                case CubeSide.Back: {
                        m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].z = (z - c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 1].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].z = (z - c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 2].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].z = (z - c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 3].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].z = (z - c_size) * m_CubeSize;
                        break;
                    }
                case CubeSide.Up: {
                        m_VerticeVec.m_Arr[start_at].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 1].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 2].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].z = (z - c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 3].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].y = (y + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].z = (z - c_size) * m_CubeSize;

                        break;
                    }
                case CubeSide.Down: {
                        m_VerticeVec.m_Arr[start_at].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 1].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 1].z = (z + c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 2].x = (x + c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 2].z = (z - c_size) * m_CubeSize;

                        m_VerticeVec.m_Arr[start_at + 3].x = (x - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].y = (y - c_size) * m_CubeSize;
                        m_VerticeVec.m_Arr[start_at + 3].z = (z - c_size) * m_CubeSize;

                        break;
                    }
            }
            m_FaceCount++;
        }
    }
}

