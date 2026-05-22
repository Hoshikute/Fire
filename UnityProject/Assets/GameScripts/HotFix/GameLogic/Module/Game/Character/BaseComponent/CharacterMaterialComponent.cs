using UnityEngine;

namespace GameLogic.Game
{
    public class CharacterMaterialComponent : ComponentBase
    {
        private Material m_material;

        public void SetMaterial(Material material)
        {
            m_material = material;
        }

        public void SetColor(Color color)
        {
            if (m_material != null)
            {
                m_material.color = color;
            }
        }

        public void SetFloat(string name, float value)
        {
            if (m_material != null)
            {
                m_material.SetFloat(name, value);
            }
        }
    }
}
