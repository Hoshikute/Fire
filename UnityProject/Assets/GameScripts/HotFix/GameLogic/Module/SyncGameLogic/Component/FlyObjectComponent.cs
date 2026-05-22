using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class FlyObjectComponent : ComponentBase
    {
        public int createrID;
        public int damage;
        public string flyObjectID;

        // FlyDataGenerate 需要从配置系统获取
        // public FlyDataGenerate FlyData { get; set; }
    }
}
