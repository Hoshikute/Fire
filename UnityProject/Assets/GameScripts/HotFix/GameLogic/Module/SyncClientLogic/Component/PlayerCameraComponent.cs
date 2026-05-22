using UnityEngine;
using GameLogic;

namespace GameLogic.SyncClientLogic.Component
{
    public class PlayerCameraComponent : ComponentBase
    {
        public Camera camera;
        public Transform target;
        public Vector3 offset = new Vector3(0, 5, -10);
        public float smoothSpeed = 5f;
    }
}
