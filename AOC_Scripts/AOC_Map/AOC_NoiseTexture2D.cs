using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UCL.Core.TextureLib;
using UCL.Core.MathLib;

namespace AOC.MapLib {
    public class AOC_NoiseTexture2D : MonoBehaviour {
        UCL_Texture2D m_Texture;
        public Vector2Int m_Size = new Vector2Int(64, 64);
        public int m_Type = 0;
        float time = 0;
        [Range(0.0f,10.0f)] public float time_offset = 1.0f;
        [Range(0.0f, 0.2f)] public float scale = 0.1f;

        RangeChecker<float> check = new RangeChecker<float>();
        private void Start() {
            m_Texture = new UCL_Texture2D(m_Size);
        }
        private void Update() {
            time += Time.deltaTime;
            
            for(int i = 0; i < m_Size.y; i++) {
                for(int j = 0; j < m_Size.x; j++) {
                    float c = 0;
                    float x = time_offset * time + scale * j;
                    float y = time_offset * time + scale * i;
                    float z = time_offset * time;
                    switch(m_Type) {
                        case 0: {
                                c = UCL.Core.MathLib.Noise.PerlinNoiseUnsigned(x, y, z);
                                //Mathf.PerlinNoise(time_offset * time + scale * j, time_offset * time + scale *i);

                                break;
                            }
                        case 1: {
                                c = Mathf.PerlinNoise(time_offset * time + scale * j, time_offset * time + scale * i);
                                //c = UCL.Core.MathLib.Noise.PerlinNoise(x, y, 0);
                                break;
                            }
                        case 2: {
                                c = UCL.Core.MathLib.Noise.PerlinNoise(x, y);
                                break;
                            }
                        case 3: {
                                //c = UCL.Core.MathLib.Noise.PerlinNoise(x,0, y);
                                break;
                            }
                    }
                    check.AddValue(c);
                    m_Texture.SetPixel(new Vector2Int(j, i), new Color(c, c, c, 1));
                }
            }
            Debug.LogWarning("check max" + check.Max + ",check min:" + check.Min);
            UCL.Core.DebugLib.UCL_DebugOnGUI.Instance.CreateData().SetOnGUIAct(() => {
                GUILayout.BeginVertical();
                GUILayout.Box(m_Texture.texture);
                GUILayout.EndVertical();
            });
        }
    }
}

