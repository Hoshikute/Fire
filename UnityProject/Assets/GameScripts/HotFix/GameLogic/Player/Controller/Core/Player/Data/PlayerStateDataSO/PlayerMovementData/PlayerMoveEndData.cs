using Animancer;
using UnityEngine;

namespace ThirdPersonController
{
    [System.Serializable]
    public class PlayerMoveEndData
    {
        [field: SerializeField] public TransitionAsset moveEnd_L { get; private set; }
        [field: SerializeField] public TransitionAsset moveEnd_R { get; private set; }
        [field: SerializeField] public ClipTransition moveToWall { get; private set; }
    }
}
