namespace GameLogic
{
    /// <summary>
    /// 移动组件
    /// </summary>
    public class MoveComponent : MomentComponentBase
    {
        public SyncVector3 pos = new SyncVector3();
        public SyncVector3 dir = new SyncVector3();
        public int m_velocity;

        public override MomentComponentBase DeepCopy()
        {
            MoveComponent mc = new MoveComponent();
            mc.ID = ID;
            mc.Frame = Frame;
            mc.pos = pos.DeepCopy();
            mc.dir = dir.DeepCopy();
            mc.m_velocity = m_velocity;
            return mc;
        }
    }
}
