using UnityEngine;

namespace GameLogic.Game
{
    public class FadInOut : MonoBehaviour
    {
        public float fadeTime = 1f;
        public bool fadeIn = true;

        private float m_timer;
        private Material m_material;

        void Update()
        {
            m_timer += Time.deltaTime;
            float alpha = fadeIn ? m_timer / fadeTime : 1f - m_timer / fadeTime;
            if (m_material != null)
            {
                m_material.SetFloat("_Alpha", alpha);
            }
        }
    }
}
