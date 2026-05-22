using UnityEngine;

namespace GameLogic.Game
{
    public class AnimComponent : ComponentBase
    {
        private Animator m_animator;

        public Animator Animator
        {
            get { return m_animator; }
            set { m_animator = value; }
        }

        public void Play(string animName)
        {
            if (m_animator != null)
            {
                m_animator.Play(animName);
            }
        }

        public void SetFloat(string name, float value)
        {
            if (m_animator != null)
            {
                m_animator.SetFloat(name, value);
            }
        }

        public void SetBool(string name, bool value)
        {
            if (m_animator != null)
            {
                m_animator.SetBool(name, value);
            }
        }

        public void SetTrigger(string name)
        {
            if (m_animator != null)
            {
                m_animator.SetTrigger(name);
            }
        }
    }
}
