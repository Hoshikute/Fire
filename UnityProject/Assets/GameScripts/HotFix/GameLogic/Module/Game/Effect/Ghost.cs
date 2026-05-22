using UnityEngine;

namespace GameLogic.Game
{
    public class Ghost : MonoBehaviour
    {
        public float lifeTime = 0.5f;
        public Material ghostMaterial;

        void Start()
        {
            Destroy(gameObject, lifeTime);
        }
    }
}
