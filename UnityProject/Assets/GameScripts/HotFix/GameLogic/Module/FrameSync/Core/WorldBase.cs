using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 同步世界基类
    /// </summary>
    public abstract class WorldBase
    {
        private SyncRule m_syncRule;
        private bool m_isStart = false;
        private bool m_isView = false;
        public bool m_isCertainty = false;
        public bool m_isRecalc = false;
        public bool m_isLocal = false;

        public SyncRule SyncRule
        {
            get => m_syncRule;
            set => m_syncRule = value;
        }

        public bool IsStart
        {
            get => m_isStart;
            set => m_isStart = value;
        }

        private int m_frameCount = 0;
        public int FrameCount
        {
            get => m_frameCount;
            set => m_frameCount = value;
        }

        private int m_entityIndex = 0;
        public int EntityIndex
        {
            get => m_entityIndex;
            set => m_entityIndex = value;
        }

        private int m_clientEntityIndex = -1;
        public int ClientEntityIndex
        {
            get => m_clientEntityIndex;
            set => m_clientEntityIndex = value;
        }

        public List<SystemBase> m_systemList = new List<SystemBase>();
        public Dictionary<int, EntityBase> m_entityDict = new Dictionary<int, EntityBase>();
        public List<EntityBase> m_entityList = new List<EntityBase>();

        public List<RecordSystemBase> m_recordList = new List<RecordSystemBase>();
        public Dictionary<string, RecordSystemBase> m_recordDict = new Dictionary<string, RecordSystemBase>();

        public Dictionary<string, SingletonComponent> m_singleCompDict = new Dictionary<string, SingletonComponent>();

        private Stack<EntityBase> m_entitiesPool = new Stack<EntityBase>();

        public event EntityChangedCallBack OnEntityCreated;
        public event EntityChangedCallBack OnEntityWillBeDestroyed;
        public event EntityChangedCallBack OnEntityDestroyed;

        public event EntityComponentChangedCallBack OnEntityComponentAdded;
        public event EntityComponentChangedCallBack OnEntityComponentRemoved;
        public event EntityComponentReplaceCallBack OnEntityComponentChange;

        public ECSEvent eventSystem = null;

        public bool isFinish = false;

        #region 重载方法

        public virtual Type[] GetSystemTypes() => new Type[0];
        public virtual Type[] GetRecordTypes() => new Type[0];
        public virtual Type[] GetRecordSystemTypes() => new Type[0];

        #endregion

        #region 生命周期

        public void Init(bool isView)
        {
            eventSystem = new ECSEvent(this);
            m_isView = isView;

            try
            {
                Type[] types = GetSystemTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    SystemBase tmp = (SystemBase)types[i].Assembly.CreateInstance(types[i].FullName);
                    m_systemList.Add(tmp);
                    tmp.m_world = this;
                    tmp.Init();
                }

                types = GetRecordTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    Type type = typeof(RecordSystem<>);
                    type = type.MakeGenericType(types[i]);

                    RecordSystemBase tmp = (RecordSystemBase)Activator.CreateInstance(type);
                    m_recordList.Add(tmp);
                    m_recordDict.Add(types[i].Name, tmp);
                    tmp.m_world = this;
                    tmp.Init();
                }

                types = GetRecordSystemTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    RecordSystemBase tmp = (RecordSystemBase)types[i].Assembly.CreateInstance(types[i].FullName);
                    m_recordList.Add(tmp);
                    tmp.m_world = this;
                    tmp.Init();
                }
            }
            catch (Exception e)
            {
                Log.Error("WorldBase Init Exception:" + e.ToString());
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < m_entityList.Count; i++)
            {
                OnEntityDestroyed?.Invoke(m_entityList[i]);
            }

            for (int i = 0; i < m_systemList.Count; i++)
            {
                m_systemList[i].Dispose();
            }

            m_entityList.Clear();
            m_entityDict.Clear();
            m_systemList.Clear();
            m_recordList.Clear();
            m_recordDict.Clear();
        }

        #endregion

        #region Update

        public void Loop(int deltaTime)
        {
            if (IsStart)
            {
                BeforeUpdate(deltaTime);
                Update(deltaTime);
            }
        }

        public void FixedLoop(int deltaTime)
        {
            if (IsStart)
            {
                Record(FrameCount);
                FrameCount++;

                NoRecalcBeforeFixedUpdate(deltaTime);
                BeforeFixedUpdate(deltaTime);
                FixedUpdate(deltaTime);
                LateFixedUpdate(deltaTime);
                NoRecalcLateFixedUpdate(deltaTime);
                LazyExecuteEntityOperation();
                EndFrame(deltaTime);
            }
        }

        public void Recalc(int frame, int deltaTime)
        {
            FrameCount++;
            OnlyCallByReCalc(frame, deltaTime);
            BeforeFixedUpdate(deltaTime);
            FixedUpdate(deltaTime);
            LateFixedUpdate(deltaTime);
            LazyExecuteEntityOperation();
        }

        void BeforeUpdate(int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].BeforeUpdate(deltaTime);
        }

        void BeforeFixedUpdate(int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].BeforeFixedUpdate(deltaTime);
        }

        void NoRecalcBeforeFixedUpdate(int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].NoRecalcBeforeFixedUpdate(deltaTime);
        }

        void Update(int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].Update(deltaTime);
        }

        public void LateUpdate(int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].LateUpdate(deltaTime);
        }

        void FixedUpdate(int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].FixedUpdate(deltaTime);
        }

        void LateFixedUpdate(int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].LateFixedUpdate(deltaTime);
        }

        void NoRecalcLateFixedUpdate(int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].NoRecalcLateFixedUpdate(deltaTime);
        }

        void EndFrame(int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].EndFrame(deltaTime);
        }

        void OnlyCallByReCalc(int frame, int deltaTime)
        {
            for (int i = 0; i < m_systemList.Count; i++)
                m_systemList[i].OnlyCallByRecalc(frame, deltaTime);
        }

        #endregion

        #region 回滚相关

        public void Record(int frame)
        {
            for (int i = 0; i < m_recordList.Count; i++)
                m_recordList[i].Record(frame);
        }

        public void RevertToFrame(int frame)
        {
            for (int i = 0; i < m_recordList.Count; i++)
                m_recordList[i].RevertToFrame(frame);
            FrameCount = frame;
        }

        public void ClearBefore(int frame)
        {
            for (int i = 0; i < m_recordList.Count; i++)
                m_recordList[i].ClearBefore(frame);
        }

        public void ClearAfter(int frame)
        {
            for (int i = 0; i < m_recordList.Count; i++)
                m_recordList[i].ClearAfter(frame);
        }

        public RecordSystemBase GetRecordSystemBase(string name)
        {
            if (!m_recordDict.ContainsKey(name))
                throw new Exception("GetRecordSystemBase error not find " + name);
            return m_recordDict[name];
        }

        #endregion

        #region 实体相关

        private List<EntityBase> m_createCache = new List<EntityBase>();
        private List<EntityBase> m_destroyCache = new List<EntityBase>();

        public void LazyExecuteEntityOperation()
        {
            for (int i = 0; i < m_createCache.Count; i++)
                AddEntity(m_createCache[i]);
            m_createCache.Clear();

            for (int i = 0; i < m_destroyCache.Count; i++)
                RemoveEntity(m_destroyCache[i]);
            m_destroyCache.Clear();
        }

        public void CreateEntity(string identifier, params ComponentBase[] comps)
        {
            identifier = FrameCount + identifier;
            CreateEntity(identifier.ToHash(), comps);
        }

        public EntityBase CreateEntity(int ID, params ComponentBase[] compList)
        {
            if (m_entityDict.ContainsKey(ID))
                throw new Exception("CreateEntity Exception: Entity ID has exist ! ->" + ID + "<-");

            EntityBase entity = NewEntity(ID, compList);
            m_createCache.Add(entity);
            return entity;
        }

        EntityBase NewEntity(int ID, params ComponentBase[] compList)
        {
            EntityBase entity = new EntityBase();
            entity.ID = ID;
            entity.World = this;

            if (compList != null)
            {
                for (int i = 0; i < compList.Length; i++)
                {
                    entity.AddComp(compList[i].GetType().Name, compList[i]);
                }
            }
            return entity;
        }

        void AddEntity(EntityBase entity)
        {
            if (m_isRecalc)
                RecalcCreateEntity(entity);
            else
                CreateEntityAndDispatch(entity);
        }

        void CreateEntityAndDispatch(EntityBase entity)
        {
            CreateEntityNoDispatch(entity);
            DispatchCreate(entity);
        }

        void DispatchCreate(EntityBase entity)
        {
            try
            {
                OnEntityCreated?.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error("DispatchCreate " + e.ToString());
            }
        }

        void CreateEntityNoDispatch(EntityBase entity)
        {
            if (m_entityDict.ContainsKey(entity.ID))
                Log.Error("CreateEntityNoDispatch 创建实体 id 冲突！ " + entity.ID);

            m_entityList.Add(entity);
            m_entityDict.Add(entity.ID, entity);

            entity.OnComponentAdded += DispatchEntityComponentAdded;
            entity.OnComponentRemoved += DispatchEntityComponentRemoved;
            entity.OnComponentReplaced += DispatchEntityComponentChange;
        }

        public void DestroyEntity(int ID)
        {
            if (!m_entityDict.ContainsKey(ID))
                throw new Exception("DestroyEntity Exception: Entity ID has not exist ! ->" + ID + "<-");

            EntityBase entity = m_entityDict[ID];
            if (!m_destroyCache.Contains(entity))
                m_destroyCache.Add(entity);
        }

        void RemoveEntity(EntityBase entity)
        {
            if (m_isRecalc)
                RecalcDestroyEntity(entity);
            else
                DestroyEntityAndDispatch(entity);
        }

        void DestroyEntityAndDispatch(EntityBase entity)
        {
            DestroyEntityNoDispatch(entity);
            DispatchDestroy(entity);
        }

        void DispatchDestroy(EntityBase entity)
        {
            try
            {
                OnEntityWillBeDestroyed?.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error("DispatchDestroy OnEntityWillBeDestroyed: " + e.ToString());
            }

            try
            {
                OnEntityDestroyed?.Invoke(entity);
            }
            catch (Exception e)
            {
                Log.Error("DispatchDestroy OnEntityDestroyed: " + e.ToString());
            }
        }

        void DestroyEntityNoDispatch(EntityBase entity)
        {
            m_entityList.Remove(entity);
            m_entityDict.Remove(entity.ID);

            entity.OnComponentAdded -= DispatchEntityComponentAdded;
            entity.OnComponentRemoved -= DispatchEntityComponentRemoved;
            entity.OnComponentReplaced -= DispatchEntityComponentChange;
        }

        #endregion

        #region 获取对象

        public int GetEntityID(string identifier)
        {
            identifier = FrameCount + identifier;
            return identifier.ToHash();
        }

        public bool GetEntityIsExist(int ID) => m_entityDict.ContainsKey(ID);

        public EntityBase GetEntity(int ID)
        {
            if (!m_entityDict.ContainsKey(ID))
                throw new Exception("GetEntity Exception: Entity ID has not exist ! ->" + ID + "<-");
            return m_entityDict[ID];
        }

        public List<EntityBase> GetEntiyList(string[] compNames)
        {
            List<EntityBase> tupleList = new List<EntityBase>();
            for (int i = 0; i < m_entityList.Count; i++)
            {
                if (GetAllExistComp(compNames, m_entityList[i]))
                    tupleList.Add(m_entityList[i]);
            }
            return tupleList;
        }

        public bool GetAllExistComp(string[] compNames, EntityBase entity)
        {
            for (int i = 0; i < compNames.Length; i++)
            {
                if (!entity.GetExistComp(compNames[i]))
                    return false;
            }
            return true;
        }

        #endregion

        #region 回滚缓存

        private List<EntityBase> m_rollbackCreateCache = new List<EntityBase>();
        private List<EntityBase> m_rollbackDestroyCache = new List<EntityBase>();

        public EntityBase RollbackDestroyEntity(int ID, params ComponentBase[] compList)
        {
            EntityBase entity = NewEntity(ID, compList);
            CreateEntityNoDispatch(entity);
            m_rollbackDestroyCache.Add(entity);
            return entity;
        }

        public void RollbackCreateEntity(int ID)
        {
            EntityBase entity = GetEntity(ID);
            DestroyEntityNoDispatch(entity);
            m_rollbackCreateCache.Add(entity);
        }

        public void EndRecalc()
        {
            for (int i = 0; i < m_rollbackCreateCache.Count; i++)
                DispatchDestroy(m_rollbackCreateCache[i]);
            m_rollbackCreateCache.Clear();

            for (int i = 0; i < m_rollbackDestroyCache.Count; i++)
                DispatchCreate(m_rollbackDestroyCache[i]);
            m_rollbackDestroyCache.Clear();
        }

        void RecalcCreateEntity(EntityBase entity)
        {
            if (GetIsExistCreateRollbackCache(entity.ID))
            {
                EntityBase cache = GetCreateRollbackCache(entity.ID);
                CopyValue(entity, cache);
                CreateEntityNoDispatch(cache);
                m_rollbackCreateCache.Remove(cache);
            }
            else
            {
                CreateEntityAndDispatch(entity);
            }
        }

        void RecalcDestroyEntity(EntityBase entity)
        {
            if (GetIsExistDestroyRollbackCache(entity.ID))
            {
                EntityBase cache = GetDestroyRollbackCache(entity.ID);
                DestroyEntityNoDispatch(cache);
                m_rollbackDestroyCache.Remove(cache);
            }
            else
            {
                DestroyEntityAndDispatch(entity);
            }
        }

        void CopyValue(EntityBase from, EntityBase to)
        {
            foreach (var item in from.m_compDict)
            {
                if (item.Value is MomentComponentBase mc)
                {
                    MomentComponentBase copy = mc.DeepCopy();
                    to.ChangeComp(item.Key, copy);
                }
            }
        }

        public EntityBase GetCreateRollbackCache(int ID)
        {
            for (int i = 0; i < m_rollbackCreateCache.Count; i++)
            {
                if (m_rollbackCreateCache[i].ID == ID)
                    return m_rollbackCreateCache[i];
            }
            throw new Exception("GetCreateRollbackCache not find " + ID);
        }

        public bool GetIsExistCreateRollbackCache(int ID)
        {
            for (int i = 0; i < m_rollbackCreateCache.Count; i++)
            {
                if (m_rollbackCreateCache[i].ID == ID)
                    return true;
            }
            return false;
        }

        public EntityBase GetDestroyRollbackCache(int ID)
        {
            for (int i = 0; i < m_rollbackDestroyCache.Count; i++)
            {
                if (m_rollbackDestroyCache[i].ID == ID)
                    return m_rollbackDestroyCache[i];
            }
            throw new Exception("GetDestroyRollbackCache not find " + ID);
        }

        public bool GetIsExistDestroyRollbackCache(int ID)
        {
            for (int i = 0; i < m_rollbackDestroyCache.Count; i++)
            {
                if (m_rollbackDestroyCache[i].ID == ID)
                    return true;
            }
            return false;
        }

        #endregion

        #region 单例组件

        public T GetSingletonComp<T>() where T : SingletonComponent, new()
        {
            Type type = typeof(T);
            string key = type.Name;

            if (type.IsGenericType)
                key += type.GetGenericArguments()[0].Name;

            if (m_singleCompDict.ContainsKey(key))
                return (T)m_singleCompDict[key];

            T comp = new T();
            comp.Init();
            m_singleCompDict.Add(key, comp);
            return comp;
        }

        public void ChangeSingletonComp<T>(T comp) where T : SingletonComponent
        {
            string compName = typeof(T).Name;
            if (m_singleCompDict.ContainsKey(compName))
                m_singleCompDict[compName] = comp;
            else
                m_singleCompDict.Add(compName, comp);
        }

        #endregion

        #region 事件派发

        void DispatchEntityComponentAdded(EntityBase entity, string compName, ComponentBase component)
        {
            OnEntityComponentAdded?.Invoke(entity, compName, component);
        }

        void DispatchEntityComponentRemoved(EntityBase entity, string compName, ComponentBase component)
        {
            OnEntityComponentRemoved?.Invoke(entity, compName, component);
        }

        void DispatchEntityComponentChange(EntityBase entity, string compName, ComponentBase previousComponent, ComponentBase newComponent)
        {
            OnEntityComponentChange?.Invoke(entity, compName, previousComponent, newComponent);
        }

        public delegate void EntityChangedCallBack(EntityBase entity);

        #endregion
    }
}
