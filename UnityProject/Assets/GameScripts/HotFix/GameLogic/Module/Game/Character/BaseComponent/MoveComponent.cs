using UnityEngine;

namespace GameLogic.Game
{
    public class MoveComponent : ComponentBase
    {
        private float m_speed = 5f;
        private Vector3 m_moveDir;

        public float Speed
        {
            get { return m_speed; }
            set { m_speed = value; }
        }

        public void Move(Vector3 dir)
        {
            m_moveDir = dir;
            if (m_character != null)
            {
                m_character.position += dir * m_speed * Time.deltaTime;
            }
        }

        public void MoveTo(Vector3 targetPos)
        {
            if (m_character != null)
            {
                Vector3 dir = (targetPos - m_character.position).normalized;
                Move(dir);
            }
        }

        public void Stop()
        {
            m_moveDir = Vector3.zero;
        }
    }
}
