using GameLogic.SyncGameLogic.Component;

namespace GameLogic.SyncGameLogic.Utils
{
    public static class GameUtils
    {
        public static string GetEventKey(int entityID, CharacterEventType eventType)
        {
            return $"{entityID}_{eventType}";
        }
    }
}
