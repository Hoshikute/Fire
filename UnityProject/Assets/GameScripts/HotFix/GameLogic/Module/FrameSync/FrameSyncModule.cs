using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 帧同步模块实现
    /// </summary>
    public class FrameSyncModule : Module, IFrameSyncModule, IUpdateModule
    {
        private List<WorldBase> m_worldList = new List<WorldBase>();
        private float m_updateTimer = 0f;
        private int m_intervalTime = 200; // 毫秒

        public int IntervalTime
        {
            get => m_intervalTime;
            set => m_intervalTime = value;
        }

        public List<WorldBase> WorldList => m_worldList;

        public override void OnInit()
        {
        }

        public override void Shutdown()
        {
            for (int i = 0; i < m_worldList.Count; i++)
            {
                m_worldList[i].Dispose();
            }
            m_worldList.Clear();
        }

        public WorldBase CreateWorld<T>() where T : WorldBase, new()
        {
            T world = new T();
            world.Init(true);
            m_worldList.Add(world);
            return world;
        }

        public void DestroyWorld(WorldBase world)
        {
            world.Dispose();
            m_worldList.Remove(world);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            int deltaTimeMs = (int)(elapseSeconds * 1000f);
            m_updateTimer += elapseSeconds * 1000f;

            // 渲染帧更新
            UpdateWorlds(deltaTimeMs);

            // 固定帧更新（帧同步核心）
            while (m_updateTimer > m_intervalTime)
            {
                FixedUpdateWorlds(m_intervalTime);
                m_updateTimer -= m_intervalTime;
            }
        }

        private void UpdateWorlds(int deltaTime)
        {
            for (int i = 0; i < m_worldList.Count; i++)
            {
                try
                {
                    m_worldList[i].Loop(deltaTime);
                }
                catch (System.Exception e)
                {
                    Log.Error("UpdateWorld Exception：" + e.ToString());
                }
            }
        }

        private void FixedUpdateWorlds(int deltaTime)
        {
            for (int i = 0; i < m_worldList.Count; i++)
            {
                try
                {
                    m_worldList[i].FixedLoop(deltaTime);
                }
                catch (System.Exception e)
                {
                    Log.Error("FixedUpdateWorld Exception：" + e.ToString());
                }
            }
        }

        public void LateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            for (int i = 0; i < m_worldList.Count; i++)
            {
                try
                {
                    m_worldList[i].LateUpdate((int)(elapseSeconds * 1000f));
                }
                catch (System.Exception e)
                {
                    Log.Error("LateUpdate Exception：" + e.ToString());
                }
            }
        }
    }
}
