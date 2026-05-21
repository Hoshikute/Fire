namespace GameLogic
{
    /// <summary>
    /// 组件基类
    /// </summary>
    public abstract class ComponentBase
    {
        private EntityBase m_entity;

        public EntityBase Entity
        {
            get => m_entity;
            set => m_entity = value;
        }

        public virtual void Init() { }
    }
}
