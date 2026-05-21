namespace GameLogic
{
    /// <summary>
    /// 所有需要网络同步的组件继承这个基类
    /// </summary>
    public abstract class MomentComponentBase : ComponentBase
    {
        private int m_id;
        private int m_frame;

        public int ID
        {
            get => m_id;
            set => m_id = value;
        }

        public int Frame
        {
            get => m_frame;
            set => m_frame = value;
        }

        public abstract MomentComponentBase DeepCopy();
    }
}
