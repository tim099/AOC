using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AOC
{
    [CreateAssetMenu(fileName = "CubeSettings", menuName = "AOC/CubeSettings")]
    public class AOC_CubeSettings : ScriptableObject
    {
        public Texture2D m_Texture;
        public Texture2D m_NormalTexture;
    }

}

