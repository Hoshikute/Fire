using GameLogic.SyncGameLogic.Component;
namespace GameLogic.Game
{
    public static class GameLogic
    {
        static bool s_isStart = false;
        static bool s_isPause = false;

        public static bool IsPause
        {
            get { return s_isPause; }
        }

        private static Camp m_camp = Camp.Team1;

        public static Camp myCamp
        {
            get { return m_camp; }
            set { m_camp = value; }
        }

        public static void Init()
        {
        }

        public static void GameStart(string GamePlayModel, string playerListInfo, string playerID, string data)
        {
            s_isStart = true;
        }

        public static void GameEnd()
        {
            s_isStart = false;
        }

        public static void Pause()
        {
            s_isPause = true;
        }

        public static void Resume()
        {
            s_isPause = false;
        }

        public static void Dispose()
        {
        }
    }

    public enum GameEventEnum
    {
        CampChange,
        GameStart,
        GameEnd
    }
}
