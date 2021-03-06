﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UCL;
namespace AOC {
    public class AOC_GameManager : UCL.Core.Game.UCL_GameManager {
        protected override void Init() {
            if(m_Inited) return;



            base.Init();
            if(!m_Inited) {//base.Init() fail!!
                return;
            }
            Debug.LogWarning("AOC_GameManager Init!!");
            Debug.LogWarning("Test4:" + m_GameConfig.GetString("Test4"));
            m_GameConfig.SetValue("Test4",System.DateTime.Now.ToString("HH:mm:ss"));
        }
    }
}