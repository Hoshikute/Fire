namespace GameLogic
{
    /// <summary>
    /// 变换组件
    /// </summary>
    public class TransformComponent : ComponentBase
    {
        public int parentID = 0;
        public SyncVector3 pos = new SyncVector3();
        public SyncVector3 dir = new SyncVector3();
    }
}
