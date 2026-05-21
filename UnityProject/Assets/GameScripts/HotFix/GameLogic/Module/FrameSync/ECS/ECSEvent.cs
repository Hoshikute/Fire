using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    public delegate void ECSEventHandle(EntityBase entity, params object[] objs);

    /// <summary>
    /// ECS事件系统
    /// </summary>
    public class ECSEvent
    {
        private WorldBase m_world;
        private Dictionary<string, ECSEventHandle> m_EventDict = new Dictionary<string, ECSEventHandle>();
        private Dictionary<string, ECSEventHandle> m_certaintyEventDict = new Dictionary<string, ECSEventHandle>();
        private List<EventCache> m_eventCache = new List<EventCache>();

        public ECSEvent(WorldBase world)
        {
            m_world = world;
        }

        public void AddListener(string key, ECSEventHandle handle, bool certainty = false)
        {
            if (!certainty)
            {
                if (m_EventDict.ContainsKey(key))
                    m_EventDict[key] += handle;
                else
                    m_EventDict.Add(key, handle);
            }
            else
            {
                if (m_certaintyEventDict.ContainsKey(key))
                    m_certaintyEventDict[key] += handle;
                else
                    m_certaintyEventDict.Add(key, handle);
            }
        }

        public void RemoveListener(string key, ECSEventHandle handle, bool certainty = false)
        {
            if (!certainty)
            {
                if (m_EventDict.ContainsKey(key))
                    m_EventDict[key] -= handle;
            }
            else
            {
                if (m_certaintyEventDict.ContainsKey(key))
                    m_certaintyEventDict[key] -= handle;
            }
        }

        public void DispatchEvent(string key, EntityBase entity, params object[] objs)
        {
            if (m_EventDict.ContainsKey(key))
            {
                try
                {
                    m_EventDict[key](entity, objs);
                }
                catch (Exception e)
                {
                    Log.Error("DispatchECSEvent " + e.ToString());
                }
            }

            if (m_world.m_isCertainty)
            {
                if (m_certaintyEventDict.ContainsKey(key))
                {
                    try
                    {
                        m_certaintyEventDict[key](entity, objs);
                    }
                    catch (Exception e)
                    {
                        Log.Error("DispatchECSEvent isCertainty " + e.ToString());
                    }
                }
            }
            else
            {
                EventCache e = new EventCache
                {
                    frame = m_world.FrameCount,
                    eventKey = key,
                    entity = entity,
                    objs = objs
                };
                m_eventCache.Add(e);
            }
        }

        public void DispatchCertainty(int frame)
        {
            for (int i = 0; i < m_eventCache.Count; i++)
            {
                EventCache e = m_eventCache[i];
                if (e.frame <= frame)
                {
                    if (m_certaintyEventDict.ContainsKey(e.eventKey))
                    {
                        m_certaintyEventDict[e.eventKey](e.entity, e.objs);
                    }
                    m_eventCache.RemoveAt(i);
                    i--;
                }
            }
        }

        public void ClearCache(int frame)
        {
            for (int i = 0; i < m_eventCache.Count; i++)
            {
                EventCache e = m_eventCache[i];
                if (e.frame <= frame)
                {
                    m_eventCache.RemoveAt(i);
                    i--;
                }
            }
        }

        public struct EventCache
        {
            public int frame;
            public string eventKey;
            public EntityBase entity;
            public object[] objs;
        }
    }
}
