using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 帧同步模块实现
    /// </summary>
    public class FrameSyncModule : Module, IFrameSyncModule, IUpdateModule
    {
        private const int SecondsToMicroseconds = 1000000;
        private const int MillisecondsToMicroseconds = 1000;

        private List<WorldBase> m_worldList = new List<WorldBase>();
        private long m_updateTimerUs = 0;
        private int m_intervalTime = FrameConfig.LogicFrameIntervalMs; // 毫秒

        public int IntervalTime
        {
            get => m_intervalTime;
            set => m_intervalTime = value > 0 ? value : 1;
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
            // 真实帧耗时只用于调度应该推进几个固定逻辑帧；逻辑计算仍只接收 m_intervalTime。
            long deltaTimeUs = (long)System.Math.Round(elapseSeconds * SecondsToMicroseconds, System.MidpointRounding.AwayFromZero);
            if (deltaTimeUs < 0)
            {
                deltaTimeUs = 0;
            }
            int deltaTimeMs = (int)(deltaTimeUs / MillisecondsToMicroseconds);
            m_updateTimerUs += deltaTimeUs;

            // 渲染帧更新
            UpdateWorlds(deltaTimeMs);

            // 固定帧更新（帧同步核心）
            long intervalTimeUs = (long)m_intervalTime * MillisecondsToMicroseconds;
            while (m_updateTimerUs >= intervalTimeUs)
            {
                FixedUpdateWorlds(m_intervalTime);
                m_updateTimerUs -= intervalTimeUs;
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
            FrameConfig.FrameId++;
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
