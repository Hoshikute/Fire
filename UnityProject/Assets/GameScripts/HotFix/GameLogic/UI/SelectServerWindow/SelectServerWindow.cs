using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 选服窗口 - 展示服务器列表并选择服务器
    /// </summary>
    [Window(UILayer.UI, "SelectServerWindow")]
    public class SelectServerWindow : UIWindow
    {
        #region UI 组件

        // 标题文本
        private Text m_text_Title;

        // 服务器列表滚动容器
        private ScrollRect m_scroll_ServerList;

        // 服务器列表容器
        private Transform m_tf_ServerContainer;

        // 服务器项模板
        private GameObject m_go_ServerItemTemplate;

        // 服务器项列表
        private List<ServerItemWidget> _serverItems = new List<ServerItemWidget>();

        #endregion

        #region 状态

        private ServerData _selectedServer;

        // 选中的颜色
        private static readonly Color s_colorSelected = new Color(0.3f, 0.6f, 0.9f, 1f);
        private static readonly Color s_colorNormal = new Color(0.2f, 0.3f, 0.5f, 0.8f);

        #endregion

        #region 生命周期

        protected override void ScriptGenerator()
        {
            // 绑定 UI 组件
            var titleTrans = transform.Find("m_text_Title");
            if (titleTrans != null)
            {
                m_text_Title = titleTrans.GetComponent<Text>();
            }

            var scrollTrans = transform.Find("m_scroll_ServerList");
            if (scrollTrans != null)
            {
                m_scroll_ServerList = scrollTrans.GetComponent<ScrollRect>();
                m_tf_ServerContainer = scrollTrans.Find("Viewport/Content");
            }

            var templateTrans = transform.Find("m_item_Server");
            if (templateTrans != null)
            {
                m_go_ServerItemTemplate = templateTrans.gameObject;
                m_go_ServerItemTemplate.SetActive(false);
            }
        }

        protected override void RegisterEvent()
        {
            // 注册事件（如有需要）
        }

        protected override void OnCreate()
        {
            // 显示鼠标
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 初始化选中服务器
            _selectedServer = ServerDataLoader.GetLocalServer();

            // 创建服务器列表
            CreateServerList();

            Log.Info("[SelectServerWindow] 选服窗口创建完成");
        }

        protected override void OnRefresh()
        {
            RefreshServerList();
        }

        protected override void OnDestroy()
        {
            // 清理服务器项
            foreach (var item in _serverItems)
            {
                if (item != null && item.gameObject != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }
            _serverItems.Clear();
        }

        #endregion

        #region 服务器列表

        private void CreateServerList()
        {
            var servers = ServerDataLoader.ServerList;
            Log.Info($"[SelectServerWindow] 创建服务器列表，数量: {servers.Count}");

            // 清理旧的列表
            foreach (var item in _serverItems)
            {
                if (item != null && item.gameObject != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }
            _serverItems.Clear();

            if (m_go_ServerItemTemplate == null || m_tf_ServerContainer == null)
            {
                Log.Error("[SelectServerWindow] 服务器项模板或容器未找到");
                return;
            }

            foreach (var server in servers)
            {
                CreateServerItem(server);
            }

            // 更新选中状态
            UpdateSelectionVisual();
        }

        private void CreateServerItem(ServerData server)
        {
            var widget = CreateWidgetByPrefab<ServerItemWidget>(m_go_ServerItemTemplate, m_tf_ServerContainer);
            if (widget != null)
            {
                widget.gameObject.name = $"ServerItem_{server.Id}";
                widget.SetData(server, OnServerSelected);
                _serverItems.Add(widget);

                // 设置服务器项的 RectTransform，确保正确布局
                var rectTransform = widget.gameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1);
                    rectTransform.sizeDelta = new Vector2(0, 60);
                }
            }
        }

        private void RefreshServerList()
        {
            var servers = ServerDataLoader.ServerList;
            Log.Info($"[SelectServerWindow] 可用服务器数量: {servers.Count}");

            foreach (var server in servers)
            {
                Log.Info($"[SelectServerWindow] 服务器: {server.Name} ({server.Address}:{server.Port})");
            }
        }

        private void OnServerSelected(ServerData server)
        {
            _selectedServer = server;
            Log.Info($"[SelectServerWindow] 选中服务器: {server.Name} ({server.Address}:{server.Port})");

            // 更新选中状态视觉
            UpdateSelectionVisual();

            // 保存选中服务器
            Game.GameData.ServerAddress = server.Address;
            Game.GameData.ServerPort = server.Port;

            // 关闭选服窗口，打开登录窗口
            GameModule.UI.CloseUI<SelectServerWindow>();
            GameModule.UI.ShowUIAsync<LoginWindow>();
        }

        private void UpdateSelectionVisual()
        {
            foreach (var item in _serverItems)
            {
                if (item != null)
                {
                    item.SetSelected(item.ServerData?.Id == _selectedServer?.Id);
                }
            }
        }

        #endregion
    }
}
