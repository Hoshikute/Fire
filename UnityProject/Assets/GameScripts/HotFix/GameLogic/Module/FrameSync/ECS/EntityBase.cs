using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 实体基类
    /// </summary>
    public class EntityBase
    {
        private WorldBase m_world;
        private int m_id = 0;

        public int ID
        {
            get => m_id;
            set => m_id = value;
        }

        public WorldBase World
        {
            get => m_world;
            set => m_world = value;
        }

        public event EntityComponentChangedCallBack OnComponentAdded;
        public event EntityComponentChangedCallBack OnComponentRemoved;
        public event EntityComponentReplaceCallBack OnComponentReplaced;

        public Dictionary<string, ComponentBase> m_compDict = new Dictionary<string, ComponentBase>();

        #region 组件操作

        public bool GetExistComp<T>() where T : ComponentBase
        {
            return GetExistComp(typeof(T).Name);
        }

        public bool GetExistComp(string compName)
        {
            return m_compDict.ContainsKey(compName);
        }

        public T AddComp<T>() where T : ComponentBase, new()
        {
            T comp = new T();
            comp.Init();
            comp.Entity = this;

            string key = typeof(T).Name;

            if (m_compDict.ContainsKey(key))
            {
                throw new Exception("AddComp exist comp ! " + key);
            }

            m_compDict.Add(key, comp);
            OnComponentAdded?.Invoke(this, key, comp);

            return comp;
        }

        public EntityBase AddComp<T>(T comp) where T : ComponentBase
        {
            string key = typeof(T).Name;
            comp.Entity = this;

            if (m_compDict.ContainsKey(key))
            {
                throw new Exception("AddComp exist comp ! " + key);
            }

            m_compDict.Add(key, comp);
            OnComponentAdded?.Invoke(this, key, comp);

            return this;
        }

        public EntityBase AddComp(string compName, ComponentBase comp)
        {
            comp.Entity = this;

            if (m_compDict.ContainsKey(compName))
            {
                throw new Exception("AddComp exist comp ! " + compName);
            }

            m_compDict.Add(compName, comp);
            OnComponentAdded?.Invoke(this, compName, comp);

            return this;
        }

        public void RemoveComp<T>() where T : ComponentBase
        {
            RemoveComp(typeof(T).Name);
        }

        public void RemoveComp(string compName)
        {
            if (!m_compDict.ContainsKey(compName))
            {
                throw new Exception("RemoveComp not exist comp ! " + compName);
            }

            ComponentBase comp = m_compDict[compName];
            m_compDict.Remove(compName);
            OnComponentRemoved?.Invoke(this, compName, comp);
            comp.Entity = null;
        }

        public T GetComp<T>() where T : ComponentBase
        {
            return (T)GetComp(typeof(T).Name);
        }

        public ComponentBase GetComp(string compName)
        {
            if (m_compDict.ContainsKey(compName))
            {
                return m_compDict[compName];
            }
            throw new Exception($"EntityID {ID} GetComp not exist comp ! {compName}");
        }

        public void ChangeComp(string compName, ComponentBase comp)
        {
            if (m_compDict.ContainsKey(compName))
            {
                ComponentBase oldComp = m_compDict[compName];
                oldComp.Entity = null;

                m_compDict[compName] = comp;
                comp.Entity = this;

                OnComponentReplaced?.Invoke(this, compName, oldComp, comp);
            }
            else
            {
                throw new Exception("ChangeComp not exist comp ! " + compName);
            }
        }

        public void ChangeComp<T>(T comp) where T : ComponentBase
        {
            ChangeComp(typeof(T).Name, comp);
        }

        #endregion
    }

    public delegate void EntityComponentChangedCallBack(EntityBase entity, string compName, ComponentBase component);
    public delegate void EntityComponentReplaceCallBack(EntityBase entity, string compName, ComponentBase previousComponent, ComponentBase newComponent);
}
