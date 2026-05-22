using UnityEngine;
using GameLogic;

namespace GameLogic.SyncClientLogic.Component
{
    public class AnimComponent : ComponentBase
    {
        public GameObject perfab;
        public GameObject waistNode;
        public Animator anim;
        public Vector3 waistDir = new Vector3(1, 0, 0);
    }
}
