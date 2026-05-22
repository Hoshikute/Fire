using UnityEngine;

namespace GameLogic.Game
{
    public class FlyObjectBase
    {
        public int createrID;
        public string flyObjectID;
        public Vector3 position;
        public Vector3 direction;
        public float speed = 20f;
        public int damage;

        public virtual void Init(int createrID, string flyObjectID, Vector3 pos, Vector3 dir)
        {
            this.createrID = createrID;
            this.flyObjectID = flyObjectID;
            position = pos;
            direction = dir;
        }

        public virtual void Update()
        {
            position += direction * speed * Time.deltaTime;
        }

        public virtual void OnHit(CharacterBase target)
        {
        }

        public virtual void OnDestroy()
        {
        }
    }
}
