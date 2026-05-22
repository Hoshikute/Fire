using UnityEngine;

namespace GameLogic.Game
{
    public class CharacterUIComponent : ComponentBase
    {
        private GameObject m_uiObject;

        public void ShowUI()
        {
            if (m_uiObject != null)
            {
                m_uiObject.SetActive(true);
            }
        }

        public void HideUI()
        {
            if (m_uiObject != null)
            {
                m_uiObject.SetActive(false);
            }
        }

        public void SetUIPosition(Vector3 position)
        {
            if (m_uiObject != null)
            {
                m_uiObject.transform.position = position;
            }
        }
    }
}
