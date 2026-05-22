using System;
using UnityEngine;

namespace GameLogic.Game
{
    [Serializable]
    public class BuffBase
    {
        public string m_buffID;
        public string m_skillID;
        public int m_count = 0;
        public bool isFinsih = false;
        public int m_createrID = 0;

        float m_time = 0;

        public void Init(string buffID, string skillID, int createrID)
        {
            m_buffID = buffID;
            m_createrID = createrID;
            m_skillID = skillID;
            m_count = 0;
            m_time = 5f; // 默认 Buff 时间
            isFinsih = false;
        }

        public void ResetBuff()
        {
            m_count++;
            m_time = 5f;
            isFinsih = false;
        }

        public void Update()
        {
            if (!isFinsih)
            {
                m_time -= Time.deltaTime;
                if (m_time < 0)
                {
                    isFinsih = true;
                }
            }
        }
    }
}
