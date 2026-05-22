using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class TransformComponent : ComponentBase
    {
        public int parentID = 0;

        public SyncVector3 pos = new SyncVector3();
        public SyncVector3 dir = new SyncVector3();
    }
}
