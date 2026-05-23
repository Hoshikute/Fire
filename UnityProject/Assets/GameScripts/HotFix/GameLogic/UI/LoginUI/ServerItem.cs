using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 服务器列表项组件
    /// 用于 SuperScrollView LoopListView2
    /// </summary>
    public class ServerItem : MonoBehaviour
    {
        [SerializeField] private Text m_txtName;
        [SerializeField] private Text m_txtAddress;
        [SerializeField] private Button m_btnSelect;
        [SerializeField] private Image m_imgBg;
        [SerializeField] private Color m_colorNormal = Color.white;
        [SerializeField] private Color m_colorSelected = new Color(0.8f, 0.9f, 1f);

        private ServerData m_data;
        private Action<ServerItem> m_onSelect;
        private bool m_isSelected;

        /// <summary>
        /// 获取服务器数据
        /// </summary>
        public ServerData Data => m_data;

        private void Awake()
        {
            if (m_btnSelect != null)
            {
                m_btnSelect.onClick.AddListener(OnSelectClick);
            }
        }

        private void OnDestroy()
        {
            if (m_btnSelect != null)
            {
                m_btnSelect.onClick.RemoveListener(OnSelectClick);
            }
        }

        /// <summary>
        /// 设置服务器数据
        /// </summary>
        public void SetData(ServerData data, Action<ServerItem> onSelect)
        {
            m_data = data;
            m_onSelect = onSelect;

            UpdateDisplay();
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            m_isSelected = selected;

            if (m_imgBg != null)
            {
                m_imgBg.color = m_isSelected ? m_colorSelected : m_colorNormal;
            }
        }

        private void UpdateDisplay()
        {
            if (m_data == null) return;

            if (m_txtName != null)
            {
                m_txtName.text = m_data.Name;
            }

            if (m_txtAddress != null)
            {
                m_txtAddress.text = $"{m_data.Address}:{m_data.Port}";
            }
        }

        private void OnSelectClick()
        {
            m_onSelect?.Invoke(this);
        }
    }
}
