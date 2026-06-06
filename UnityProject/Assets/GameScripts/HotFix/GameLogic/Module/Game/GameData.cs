using System.Collections.Generic;

namespace GameLogic.Game
{
    public static class GameData
    {
        static List<int> s_choiceList = new List<int>();

        public static List<int> ChoiceList
        {
            get { return s_choiceList; }
            set { s_choiceList = value; }
        }

        // 服务器连接信息
        public static string ServerAddress { get; set; } = "127.0.0.1";
        public static int ServerPort { get; set; } = 7500;
        public static string PlayerName { get; set; } = "Player";
        public static string PlayerId { get; set; } = string.Empty;
        public static string PlayerCharacterId { get; set; } = "1";

        public static void Init()
        {
        }

        public static void ClearData()
        {
            s_choiceList.Clear();
            PlayerId = string.Empty;
            PlayerCharacterId = "1";
        }

        public static void Dispose()
        {
        }
    }

    public enum GameDataEvent
    {
        RankList,
        ElementList,
        ChoiceList,
        ItemList
    }
}
