using System.Collections.Generic;
using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class RankComponent : SingletonComponent
    {
        public List<PlayerComponent> rankList = new List<PlayerComponent>();
    }
}
