using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class CampComponent : ComponentBase
    {
        public int creater;
        public Camp camp = Camp.Team1;
    }

    public enum Camp
    {
        Team1,
        Team2,
        Neutral
    }
}
