using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UCL.MeshLib {
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class UCL_MeshCreator : MonoBehaviour {
        public UnityEngine.Rendering.IndexFormat m_IndexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        public List<Vector3> m_Vertices = new List<Vector3>();
        public List<int> m_Triangles = new List<int>();
        public List<int> m_Triangles2 = new List<int>();
        public List<Vector2> m_UV = new List<Vector2>();

        protected MeshFilter m_MeshFilter;
        protected MeshRenderer m_MeshRenderer;
        protected Mesh m_Mesh;
        virtual public void Init() {
            m_MeshRenderer = GetComponent<MeshRenderer>();
            if(m_MeshRenderer == null) {
                m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            m_MeshFilter = GetComponent<MeshFilter>();
            if(m_MeshFilter == null) {
                m_MeshFilter = gameObject.AddComponent<MeshFilter>();
            }
            if(m_MeshFilter.mesh == null) {
                m_MeshFilter.mesh = new Mesh();
            }
            m_Mesh = m_MeshFilter.mesh;
            m_Mesh.indexFormat = m_IndexFormat;
            m_Mesh.Clear();

            using(UCL_IntArray arr = new UCL_IntArray(10)) {
                Debug.LogWarning("arr");
            }
            m_Mesh.subMeshCount = 2;
            Debug.LogWarning("arr2");
            m_Mesh.vertices = m_Vertices.ToArray();
            //m_Mesh.SetTriangles(m_Triangles.ToArray(), 0,true, 0); //SetTriangles(int[] triangles, int trianglesStart
            m_Mesh.SetTriangles(m_Triangles, 0);
            m_Mesh.SetTriangles(m_Triangles2, 1);
            //m_Mesh.triangles = m_Triangles.ToArray();
            //SetTriangles(m_Triangles.ToArray());
            m_Mesh.uv = m_UV.ToArray(); // add this line to the code here
            m_Mesh.Optimize();
            m_Mesh.RecalculateNormals();

        }
        /*
        virtual public void SetTriangles(System.Array arr) {
            //SetTrianglesImpl
            var SetTrianglesImpl = typeof(Mesh).GetMethod("SetTrianglesImpl", BindingFlags.NonPublic | BindingFlags.Instance);
            if(SetTrianglesImpl != null) {
                SetTrianglesImpl?.Invoke(m_Mesh, new object[] { -1, UnityEngine.Rendering.IndexFormat.UInt32, arr, arr.Length, 0, arr.Length, true, 0 });
            } else {
                Debug.LogError("SetTrianglesImpl Not Found!!");
            }
            //m_Mesh.triangles = arr;
        }
        */
        void Start() {
            Init();
        }

        // Update is called once per frame
        void Update() {

        }
    }
}