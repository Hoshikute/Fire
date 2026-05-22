using UnityEngine;

namespace GameLogic.Game
{
    public class SM_UVScroller : MonoBehaviour
    {
        public float scrollSpeed = 1f;
        public Vector2 direction = Vector2.right;

        private Renderer m_renderer;
        private Vector2 m_offset;

        void Start()
        {
            m_renderer = GetComponent<Renderer>();
        }

        void Update()
        {
            if (m_renderer != null)
            {
                m_offset += direction * scrollSpeed * Time.deltaTime;
                m_renderer.material.SetTextureOffset("_MainTex", m_offset);
            }
        }
    }
}
