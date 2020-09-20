using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AOC.MapLib {
    [UCL.Core.ATTR.EnableUCLEditor]
    //[UCL.Core.ATTR.RequiresConstantRepaint]
    public class AOC_NoiseTexture2D : MonoBehaviour {
        UCL.Core.TextureLib.UCL_Texture2D m_Texture;
        public Vector2Int m_Size = new Vector2Int(64, 64);

        float m_Time = 0;
        [Range(0, 3)] public int m_Type = 0;
        [Range(0.0f,10.0f)] public float m_TimeOffSet = 1.0f;
        [Range(0.0f, 0.2f)] public float m_Scale = 0.1f;
        bool m_OnValidated = false;
        //RangeChecker<float> check = new RangeChecker<float>();
        private void Start() {
            m_Texture = new UCL.Core.TextureLib.UCL_Texture2D(m_Size);
        }
        private void OnValidate() {
            m_OnValidated = true;
        }
        [UCL.Core.ATTR.UCL_DrawTexture2D]
        public UCL.Core.TextureLib.UCL_Texture2D NoiseTexture() {
            if(!enabled) return null;

            if(m_Texture == null) {
                m_Texture = new UCL.Core.TextureLib.UCL_Texture2D(m_Size);
                UpdateTexture();
            } else if(m_OnValidated) {
                m_OnValidated = false;
                UpdateTexture();
            }

            return m_Texture;
        }
        void UpdateTexture() {
            for(int i = 0; i < m_Size.y; i++) {
                for(int j = 0; j < m_Size.x; j++) {
                    float c = 0;
                    float x = m_TimeOffSet * m_Time + m_Scale * j;
                    float y = m_TimeOffSet * m_Time + m_Scale * i;
                    float z = m_TimeOffSet * m_Time;
                    switch(m_Type) {
                        case 0: {
                                c = UCL.Core.MathLib.Noise.PerlinNoiseUnsigned(m_Scale * j, m_Scale * i, z);
                                //Mathf.PerlinNoise(time_offset * time + scale * j, time_offset * time + scale *i);

                                break;
                            }
                        case 1: {
                                c = Mathf.PerlinNoise(m_TimeOffSet * m_Time + m_Scale * j, m_TimeOffSet * m_Time + m_Scale * i);
                                //c = UCL.Core.MathLib.Noise.PerlinNoise(x, y, 0);
                                break;
                            }
                        case 2: {
                                c = 0.5f*UCL.Core.MathLib.Noise.PerlinNoise(x, y)+0.5f;
                                break;
                            }
                        case 3: {
                                c = 0.5f * UCL.Core.MathLib.Noise.PerlinNoise(x,0, y) + 0.5f;
                                break;
                            }
                    }
                    //check.AddValue(c);
                    m_Texture.SetPixel(new Vector2Int(j, i), new Color(c, c, c, 1));
                }
            }
        }
        private void Update() {
            m_Time += Time.deltaTime;
            UpdateTexture();

            //Debug.LogWarning("check max" + check.Max + ",check min:" + check.Min);
            UCL.Core.DebugLib.UCL_DebugOnGUI.Instance.CreateData().AddOnGUIAct(() => {
                GUILayout.BeginVertical();
                GUILayout.Box(m_Texture.texture);
                GUILayout.EndVertical();
            });
        }
    }
}

