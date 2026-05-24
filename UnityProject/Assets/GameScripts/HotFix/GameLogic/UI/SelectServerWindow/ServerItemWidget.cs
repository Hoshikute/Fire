using System;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 服务器项 Widget - 展示单个服务器信息
    /// </summary>
    public class ServerItemWidget : UIWidget
    {
        #region UI 组件

        // 服务器名称文本
        private Text m_text_Name;

        // 背景图片
        private Image m_img_Background;

        // 选择按钮
        private Button m_btn_Select;

        #endregion

        #region 数据

        private ServerData _serverData;
        private Action<ServerData> _onSelected;

        // 选中的颜色
        private static readonly Color s_colorSelected = new Color(0.3f, 0.6f, 0.9f, 1f);
        private static readonly Color s_colorNormal = new Color(0.2f, 0.3f, 0.5f, 0.8f);

        public ServerData ServerData => _serverData;

        #endregion

        #region 生命周期

        protected override void ScriptGenerator()
        {
            // 绑定 UI 组件
            m_text_Name = gameObject.GetComponentInChildren<Text>();
            m_img_Background = gameObject.GetComponent<Image>();
            m_btn_Select = gameObject.GetComponent<Button>();

            if (m_btn_Select == null)
            {
                m_btn_Select = gameObject.AddComponent<Button>();
            }
        }

        protected override void RegisterEvent()
        {
            if (m_btn_Select != null)
            {
                m_btn_Select.onClick.AddListener(OnButtonClick);
            }
        }

        protected override void OnDestroy()
        {
            _onSelected = null;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置服务器数据
        /// </summary>
        public void SetData(ServerData server, Action<ServerData> onSelected)
        {
            _serverData = server;
            _onSelected = onSelected;

            if (m_text_Name != null)
            {
                m_text_Name.text = $"{server.Name}\n{server.Address}:{server.Port}";
            }
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (m_img_Background != null)
            {
                m_img_Background.color = selected ? s_colorSelected : s_colorNormal;
            }
        }

        #endregion

        #region 按钮事件

        private void OnButtonClick()
        {
            _onSelected?.Invoke(_serverData);
        }

        #endregion
    }
}
