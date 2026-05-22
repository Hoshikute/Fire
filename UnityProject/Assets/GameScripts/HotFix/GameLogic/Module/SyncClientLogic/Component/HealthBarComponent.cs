using UnityEngine;
using GameLogic;

namespace GameLogic.SyncClientLogic.Component
{
    public class HealthBarComponent : ComponentBase
    {
        public GameObject healthBarGo;
        public Transform healthBarTrans;
        public Vector3 offset = new Vector3(0, 2f, 0);
    }
}
