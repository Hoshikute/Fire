using GameLogic.Common.Pool;
using System.Collections.Generic;
using UnityEngine;
using GameLogic;

namespace GameLogic.SyncClientLogic.Component
{
    public class PerfabComponent : ComponentBase
    {
        public GameObject perfab;
        public HardPointComponent hardPoint;
        public List<PoolObject> followEffect = new List<PoolObject>();
    }
}
