using Animancer;
using UnityEngine;

namespace ThirdPersonController
{
    [System.Serializable]
    public class PlayerMoveLoopData
    {
        [field: SerializeField] public TransitionAsset moveLoop { get; private set; }
    }
}
