namespace GameLogic
{
    /// <summary>
    /// 记录系统基类，用于回滚机制
    /// </summary>
    public abstract class RecordSystemBase : SystemBase
    {
        public abstract void Record(int frame, EntityBase entity);
        public abstract void Record(int frame);
        public abstract void RevertToFrame(int frame);
        public abstract void ClearAfter(int frame);
        public abstract void ClearBefore(int frame);
        public abstract MomentComponentBase GetRecord(int id, int frame);
        public abstract void PrintRecord(int id);
    }
}
