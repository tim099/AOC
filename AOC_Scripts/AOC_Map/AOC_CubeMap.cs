using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AOC.MapLib {
    public class AOC_CubeMap : MonoBehaviour {
        public static AOC_CubeMap ins = null;

        public AOC_CubeMapSeg m_SegTmp = null;
        public Vector3Int m_MapSize = new Vector3Int(256, 128, 256);
        public Vector3Int m_MapSegSize = new Vector3Int(32, 128, 32);
        public Vector3Int m_MapSegCount = Vector3Int.one;//Auto Gen
        private void Awake() {
            Init();
        }
        virtual public void Init() {
            ins = this;
        }
    }
}