using UnityEngine;
using System.Collections.Generic;

namespace GameLogic.Common.Timer
{
    public class Timer
    {
        public static List<TimerEvent> m_timers = new List<TimerEvent>();
        public static List<TimerEvent> m_removeList = new List<TimerEvent>();

        private static bool s_initialized = false;

        public static void Init()
        {
            if (s_initialized) return;
            s_initialized = true;

            // 使用 GameModule.Timer 或 MonoBehaviour 驱动
            // 这里使用 Unity 的 update 驱动，通过 TimerDriver 组件
            var driver = TimerDriver.GetOrCreate();
            driver.OnUpdate += Update;
        }

        static void Update()
        {
            for (int i = 0; i < m_timers.Count; i++)
            {
                m_timers[i].Update();

                if (m_timers[i].m_isDone)
                {
                    TimerEvent e = m_timers[i];
                    e.CompleteTimer();

                    if (e.m_repeatCount == 0)
                    {
                        m_removeList.Add(e);
                    }
                }
            }

            for (int i = 0; i < m_removeList.Count; i++)
            {
                m_timers.Remove(m_removeList[i]);
            }

            m_removeList.Clear();
        }

        public static bool GetIsExistTimer(string timerName)
        {
            for (int i = 0; i < m_timers.Count; i++)
            {
                if (m_timers[i].m_timerName == timerName)
                {
                    return true;
                }
            }
            return false;
        }

        public static TimerEvent GetTimer(string timerName)
        {
            for (int i = 0; i < m_timers.Count; i++)
            {
                if (m_timers[i].m_timerName == timerName)
                {
                    return m_timers[i];
                }
            }
            throw new System.Exception("Get Timer Exception not find ->" + timerName + "<-");
        }

        /// <summary>
        /// 延迟调用
        /// </summary>
        public static TimerEvent DelayCallBack(float delayTime, TimerCallBack callBack, params object[] objs)
        {
            return AddTimer(delayTime, false, 0, null, callBack, objs);
        }

        /// <summary>
        /// 延迟调用
        /// </summary>
        public static TimerEvent DelayCallBack(float delayTime, bool isIgnoreTimeScale, TimerCallBack callBack, params object[] objs)
        {
            return AddTimer(delayTime, isIgnoreTimeScale, 0, null, callBack, objs);
        }

        /// <summary>
        /// 间隔一定时间重复调用
        /// </summary>
        public static TimerEvent CallBackOfIntervalTimer(float spaceTime, TimerCallBack callBack, params object[] objs)
        {
            return AddTimer(spaceTime, false, -1, null, callBack, objs);
        }

        /// <summary>
        /// 间隔一定时间重复调用
        /// </summary>
        public static TimerEvent CallBackOfIntervalTimer(float spaceTime, bool isIgnoreTimeScale, TimerCallBack callBack, params object[] objs)
        {
            return AddTimer(spaceTime, isIgnoreTimeScale, -1, null, callBack, objs);
        }

        /// <summary>
        /// 间隔一定时间重复调用
        /// </summary>
        public static TimerEvent CallBackOfIntervalTimer(float spaceTime, bool isIgnoreTimeScale, string timerName, TimerCallBack callBack, params object[] objs)
        {
            return AddTimer(spaceTime, isIgnoreTimeScale, -1, timerName, callBack, objs);
        }

        /// <summary>
        /// 添加一个Timer
        /// </summary>
        public static TimerEvent AddTimer(float spaceTime, bool isIgnoreTimeScale, int callBackCount, string timerName, TimerCallBack callBack, params object[] objs)
        {
            TimerEvent te = new TimerEvent();
            te.m_timerName = timerName ?? te.GetHashCode().ToString();
            te.m_currentTimer = 0;
            te.m_timerSpace = spaceTime;
            te.m_callBack = callBack;
            te.m_objs = objs;
            te.m_isIgnoreTimeScale = isIgnoreTimeScale;
            te.m_repeatCount = callBackCount;
            m_timers.Add(te);
            return te;
        }

        public static void DestroyTimer(TimerEvent timer, bool isCallBack = false)
        {
            if (m_timers.Contains(timer))
            {
                if (isCallBack)
                {
                    timer.CallBackTimer();
                }
                m_timers.Remove(timer);
            }
            else
            {
                Debug.LogError("Timer DestroyTimer error: dont exist timer " + timer);
            }
        }

        public static void DestroyTimer(string timerName, bool isCallBack = false)
        {
            for (int i = m_timers.Count - 1; i >= 0; i--)
            {
                if (m_timers[i].m_timerName.Equals(timerName))
                {
                    DestroyTimer(m_timers[i], isCallBack);
                }
            }
        }

        public static void DestroyAllTimer(bool isCallBack = false)
        {
            for (int i = 0; i < m_timers.Count; i++)
            {
                if (isCallBack)
                {
                    m_timers[i].CallBackTimer();
                }
            }
            m_timers.Clear();
        }

        public static void ResetTimer(TimerEvent timer)
        {
            if (m_timers.Contains(timer))
            {
                timer.ResetTimer();
            }
            else
            {
                Debug.LogError("Timer ResetTimer error: dont exist timer " + timer);
            }
        }

        public static void ResetTimer(string timerName)
        {
            for (int i = 0; i < m_timers.Count; i++)
            {
                if (m_timers[i].m_timerName.Equals(timerName))
                {
                    ResetTimer(m_timers[i]);
                }
            }
        }
    }

    /// <summary>
    /// Timer 驱动组件
    /// </summary>
    public class TimerDriver : MonoBehaviour
    {
        private static TimerDriver s_instance;

        public event System.Action OnUpdate;

        public static TimerDriver GetOrCreate()
        {
            if (s_instance == null)
            {
                var go = new GameObject("[TimerDriver]");
                DontDestroyOnLoad(go);
                s_instance = go.AddComponent<TimerDriver>();
            }
            return s_instance;
        }

        private void Update()
        {
            OnUpdate?.Invoke();
        }
    }
}
