using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 登录界面 - 服务器选择与玩家名称
    /// </summary>
    [Window(UILayer.UI, "LoginUI")]
    public class LoginUI : UIWindow
    {
        #region UI 组件

        // 玩家名称输入框
        private InputField m_inputPlayerName;

        // 进入游戏按钮
        private Button m_btnPlay;

        // 服务器列表容器
        private Transform m_serverListContainer;

        // 服务器按钮预制体
        private GameObject m_serverItemTemplate;

        // 服务器按钮列表
        private List<GameObject> m_serverItems = new List<GameObject>();

        // 服务器项图片列表（用于更新选中状态）
        private List<Image> m_serverItemImages = new List<Image>();

        #endregion

        #region 状态

        private ServerData _selectedServer;
        private string _playerName;

        // 选中的颜色
        private static readonly Color s_colorSelected = new Color(0.3f, 0.6f, 0.9f, 1f);
        private static readonly Color s_colorNormal = new Color(0.2f, 0.3f, 0.5f, 0.8f);

        // 随机名字库
        private static readonly string[] s_firstNames = { "勇敢的", "聪明的", "快速的", "强大的", "神秘的", "传说中的", "无敌的", "闪耀的" };
        private static readonly string[] s_lastNames = { "战士", "法师", "弓箭手", "骑士", "刺客", "牧师", "术士", "武僧" };

        #endregion

        #region 生命周期

        protected override void ScriptGenerator()
        {
            // 绑定 UI 组件
            var inputAccount = transform.Find("m_inputAccount");
            if (inputAccount != null)
            {
                m_inputPlayerName = inputAccount.GetComponent<InputField>();
            }

            var btnLogin = transform.Find("m_btnLogin");
            if (btnLogin != null)
            {
                m_btnPlay = btnLogin.GetComponent<Button>();
            }

            // 查找或创建服务器列表容器
            m_serverListContainer = transform.Find("m_panel_server");
            if (m_serverListContainer == null)
            {
                // 如果不存在，创建一个容器
                var containerObj = new GameObject("m_panel_server");
                containerObj.transform.SetParent(transform, false);
                var rectTransform = containerObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(0, 50);
                rectTransform.sizeDelta = new Vector2(400, 300);
                containerObj.AddComponent<Image>();
                var layoutGroup = containerObj.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                layoutGroup.spacing = 10;
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false;
                m_serverListContainer = containerObj.transform;
            }

            // 创建服务器项模板
            CreateServerItemTemplate();
        }

        protected override void RegisterEvent()
        {
            if (m_btnPlay != null)
            {
                m_btnPlay.onClick.AddListener(OnPlayClick);
            }
        }

        protected override void OnCreate()
        {
            // 显示鼠标
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 初始化选中服务器
            _selectedServer = ServerDataLoader.GetLocalServer();

            // 生成随机名字
            GenerateRandomName();

            // 创建服务器列表
            CreateServerList();

            Log.Info("[LoginUI] 登录界面创建完成");
        }

        protected override void OnRefresh()
        {
            RefreshServerList();
        }

        protected override void OnDestroy()
        {
            // 清理服务器按钮
            foreach (var item in m_serverItems)
            {
                if (item != null)
                {
                    Object.Destroy(item);
                }
            }
            m_serverItems.Clear();
            m_serverItemImages.Clear();
        }

        #endregion

        #region 服务器列表

        private void CreateServerItemTemplate()
        {
            // 创建模板按钮
            m_serverItemTemplate = new GameObject("ServerItemTemplate");
            m_serverItemTemplate.SetActive(false);

            var rectTransform = m_serverItemTemplate.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(350, 60);

            var image = m_serverItemTemplate.AddComponent<Image>();
            image.color = s_colorNormal;

            var button = m_serverItemTemplate.AddComponent<Button>();

            // 创建文字
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(m_serverItemTemplate.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        private void CreateServerList()
        {
            var servers = ServerDataLoader.ServerList;
            Log.Info($"[LoginUI] 创建服务器列表，数量: {servers.Count}");

            // 清理旧的列表
            foreach (var item in m_serverItems)
            {
                if (item != null)
                {
                    Object.Destroy(item);
                }
            }
            m_serverItems.Clear();
            m_serverItemImages.Clear();

            foreach (var server in servers)
            {
                CreateServerButton(server);
            }

            // 更新选中状态
            UpdateSelectionVisual();
        }

        private void CreateServerButton(ServerData server)
        {
            var itemObj = Object.Instantiate(m_serverItemTemplate, m_serverListContainer);
            itemObj.name = $"ServerItem_{server.Id}";
            itemObj.SetActive(true);

            var rectTransform = itemObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(350, 60);

            var image = itemObj.GetComponent<Image>();
            var button = itemObj.GetComponent<Button>();

            // 设置文字
            var text = itemObj.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = $"{server.Name}\n{server.Address}:{server.Port}";
            }

            // 添加点击事件
            ServerData capturedServer = server; // 闭包捕获
            button.onClick.AddListener(() =>
            {
                SelectServer(capturedServer);
            });

            m_serverItems.Add(itemObj);
            m_serverItemImages.Add(image);
        }

        private void RefreshServerList()
        {
            var servers = ServerDataLoader.ServerList;
            Log.Info($"[LoginUI] 可用服务器数量: {servers.Count}");

            foreach (var server in servers)
            {
                Log.Info($"[LoginUI] 服务器: {server.Name} ({server.Address}:{server.Port})");
            }
        }

        private void SelectServer(ServerData server)
        {
            _selectedServer = server;
            Log.Info($"[LoginUI] 选中服务器: {server.Name} ({server.Address}:{server.Port})");

            // 更新选中状态视觉
            UpdateSelectionVisual();

            // 选择服务器后直接加载游戏场景
            LoadGameScene().Forget();
        }

        private void UpdateSelectionVisual()
        {
            for (int i = 0; i < m_serverItems.Count && i < m_serverItemImages.Count; i++)
            {
                var text = m_serverItems[i].GetComponentInChildren<Text>();
                if (text != null && m_serverItemImages[i] != null)
                {
                    bool isSelected = text.text.Contains(_selectedServer?.Name ?? "");
                    m_serverItemImages[i].color = isSelected ? s_colorSelected : s_colorNormal;
                }
            }
        }

        #endregion

        #region 按钮事件

        private void OnPlayClick()
        {
            Log.Info("[LoginUI] 点击进入游戏");

            // 获取玩家名称
            _playerName = m_inputPlayerName?.text;
            if (string.IsNullOrEmpty(_playerName))
            {
                _playerName = GenerateRandomName();
                if (m_inputPlayerName != null)
                {
                    m_inputPlayerName.text = _playerName;
                }
            }

            // 加载游戏场景
            LoadGameScene().Forget();
        }

        private async UniTaskVoid LoadGameScene()
        {
            Log.Info("[LoginUI] 进入游戏...");

            // 保存选中服务器
            if (_selectedServer != null)
            {
                Game.GameData.ServerAddress = _selectedServer.Address;
                Game.GameData.ServerPort = _selectedServer.Port;
                Game.GameData.PlayerName = _playerName;

                Log.Info($"[LoginUI] 服务器: {_selectedServer.Address}:{_selectedServer.Port}");
                Log.Info($"[LoginUI] 玩家: {_playerName}");
            }

            // 关闭登录界面
            GameModule.UI.CloseUI<LoginUI>();

            // 加载 Game 场景
            await GameModule.Scene.LoadSceneAsync("Game");

            // 初始化 Game 场景（设置相机 + 加载 Player）
            await GameModule.BattleContext.InitializeGameScene();

            Log.Info("[LoginUI] 进入游戏完成");
        }

        #endregion

        #region 随机名字

        private string GenerateRandomName()
        {
            string firstName = s_firstNames[Random.Range(0, s_firstNames.Length)];
            string lastName = s_lastNames[Random.Range(0, s_lastNames.Length)];
            string number = Random.Range(100, 999).ToString();

            _playerName = $"{firstName}{lastName}{number}";
            return _playerName;
        }

        #endregion
    }
}
