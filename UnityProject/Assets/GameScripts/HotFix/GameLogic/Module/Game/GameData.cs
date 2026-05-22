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

        public static void Init()
        {
        }

        public static void ClearData()
        {
            s_choiceList.Clear();
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
