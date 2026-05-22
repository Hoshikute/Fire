using System;
using System.Collections.Generic;
using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    [Serializable]
    public class ItemCreatePointComponent : ComponentBase
    {
        public SyncVector3 pos = new SyncVector3();
        public List<string> randomList = new List<string>();

        public int CreateTimer = 0;
        public int CreateItemID;
    }
}
