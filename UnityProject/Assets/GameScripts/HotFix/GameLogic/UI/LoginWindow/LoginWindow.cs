using System.Collections.Generic;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 登录窗口 - 匿名会话入场
    /// </summary>
    [Window(UILayer.UI, "LoginWindow")]
    public class LoginWindow : UIWindow
    {
        #region UI 组件

        private Text m_text_Title;
        private InputField m_input_Account;
        private InputField m_input_Password;
        private Button m_btn_RandomName;
        private Button m_btn_Login;

        #endregion

        #region 状态

        private string _playerName;
        private bool _isEntering;
        private bool _loginRequestSent;

        private static readonly string[] s_firstNames = { "勇敢的", "聪明的", "快速的", "强大的", "神秘的", "传说中的", "无敌的", "闪耀的" };
        private static readonly string[] s_lastNames = { "战士", "法师", "弓箭手", "骑士", "刺客", "牧师", "术士", "武僧" };

        #endregion

        #region 生命周期

        protected override void ScriptGenerator()
        {
            var titleTrans = transform.Find("m_text_Title");
            if (titleTrans != null)
            {
                m_text_Title = titleTrans.GetComponent<Text>();
            }

            var accountTrans = transform.Find("m_input_Account");
            if (accountTrans != null)
            {
                m_input_Account = accountTrans.GetComponent<InputField>();
            }

            var passwordTrans = transform.Find("m_input_Password");
            if (passwordTrans != null)
            {
                m_input_Password = passwordTrans.GetComponent<InputField>();
            }

            var randomNameTrans = transform.Find("m_btn_RandomName");
            if (randomNameTrans != null)
            {
                m_btn_RandomName = randomNameTrans.GetComponent<Button>();
            }

            var loginTrans = transform.Find("m_btn_Login");
            if (loginTrans != null)
            {
                m_btn_Login = loginTrans.GetComponent<Button>();
            }
        }

        protected override void RegisterEvent()
        {
            if (m_btn_RandomName != null)
            {
                m_btn_RandomName.onClick.AddListener(OnRandomNameClick);
            }

            if (m_btn_Login != null)
            {
                m_btn_Login.onClick.AddListener(OnLoginClick);
            }
        }

        protected override void OnCreate()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            GenerateRandomName();
            SetupAnonymousLoginView();
            SubscribeNetworkEvents();

            Log.Info("[LoginWindow] 匿名登录窗口创建完成");
        }

        protected override void OnRefresh()
        {
        }

        protected override void OnDestroy()
        {
            UnsubscribeNetworkEvents();
        }

        #endregion

        #region 按钮事件

        private void OnRandomNameClick()
        {
            GenerateRandomName();
            Log.Info($"[LoginWindow] 生成本地显示名: {_playerName}");
        }

        private void OnLoginClick()
        {
            if (_isEntering)
            {
                Log.Warning("[LoginWindow] 正在进入游戏，请勿重复点击");
                return;
            }

            _playerName = m_input_Account?.text;
            if (string.IsNullOrEmpty(_playerName))
            {
                _playerName = GenerateRandomName();
                if (m_input_Account != null)
                {
                    m_input_Account.text = _playerName;
                }
            }

            BeginAnonymousLogin();
        }

        #endregion

        #region 登录流程

        private void BeginAnonymousLogin()
        {
            _isEntering = true;
            _loginRequestSent = false;

            EnsureNetworkInitialized();

            Log.Info("[LoginWindow] 开始匿名会话登录");
            Log.Info($"[LoginWindow] 本地显示名: {_playerName}");
            Log.Info($"[LoginWindow] 服务器: {Game.GameData.ServerAddress}:{Game.GameData.ServerPort}");

            if (GameModule.Network.IsConnected)
            {
                SendAnonymousLogin();
                return;
            }

            GameModule.Network.Connect(Game.GameData.ServerAddress, Game.GameData.ServerPort);
        }

        private void EnsureNetworkInitialized()
        {
            if (!GameModule.Network.IsInitialized)
            {
                GameModule.Network.Init<ProtocolService>(ProtocolType.Tcp);
            }
        }

        private void SendAnonymousLogin()
        {
            if (_loginRequestSent)
            {
                return;
            }

            _loginRequestSent = true;
            GameModule.Network.SendMessage("playerloginmsg", new Dictionary<string, object>());
            Log.Info("[LoginWindow] 已发送匿名登录请求");
        }

        private void SubscribeNetworkEvents()
        {
            GameModule.Network.StatusChanged += OnNetworkStatusChanged;
            GameModule.Network.MessageReceived += OnNetworkMessageReceived;
        }

        private void UnsubscribeNetworkEvents()
        {
            GameModule.Network.StatusChanged -= OnNetworkStatusChanged;
            GameModule.Network.MessageReceived -= OnNetworkMessageReceived;
        }

        private void OnNetworkStatusChanged(NetworkState status)
        {
            if (!_isEntering)
            {
                return;
            }

            if (status == NetworkState.Connected)
            {
                SendAnonymousLogin();
                return;
            }

            if (status == NetworkState.FaildToConnect || status == NetworkState.ConnectBreak)
            {
                _isEntering = false;
                _loginRequestSent = false;
                Log.Error($"[LoginWindow] 匿名登录失败，网络状态: {status}");
            }
        }

        private void OnNetworkMessageReceived(NetWorkMessage message)
        {
            if (!_isEntering || message == null || message.m_MessageType != "playerloginmsg")
            {
                return;
            }

            int code = message.m_data.ContainsKey("code0") ? (int)message.m_data["code0"] : -1;
            if (code != 0)
            {
                _isEntering = false;
                _loginRequestSent = false;
                Log.Error($"[LoginWindow] 服务端拒绝登录，code={code}");
                return;
            }

            string assignedPlayerId = message.m_data.ContainsKey("playerid")
                ? message.m_data["playerid"].ToString()
                : string.Empty;
            string assignedCharacterId = message.m_data.ContainsKey("characterid")
                ? message.m_data["characterid"].ToString()
                : "1";

            Game.GameData.PlayerId = assignedPlayerId;
            Game.GameData.PlayerName = assignedPlayerId;
            Game.GameData.PlayerCharacterId = assignedCharacterId;

            _isEntering = false;
            _loginRequestSent = false;

            Log.Info($"[LoginWindow] 服务端分配玩家ID: {assignedPlayerId}");
            LoadGameScene().Forget();
        }

        #endregion

        #region 视图

        private void SetupAnonymousLoginView()
        {
            if (m_text_Title != null)
            {
                m_text_Title.text = "匿名进入游戏";
            }

            if (m_input_Password != null)
            {
                m_input_Password.gameObject.SetActive(false);
            }

            SetInputPlaceholder(m_input_Account, "显示名（可选）");
        }

        private void SetInputPlaceholder(InputField inputField, string text)
        {
            if (inputField == null)
            {
                return;
            }

            Transform placeholder = inputField.transform.Find("Placeholder");
            if (placeholder == null)
            {
                return;
            }

            Text placeholderText = placeholder.GetComponent<Text>();
            if (placeholderText != null)
            {
                placeholderText.text = text;
            }
        }

        #endregion

        #region 随机名字

        private string GenerateRandomName()
        {
            string firstName = s_firstNames[Random.Range(0, s_firstNames.Length)];
            string lastName = s_lastNames[Random.Range(0, s_lastNames.Length)];
            string number = Random.Range(100, 999).ToString();

            _playerName = $"{firstName}{lastName}{number}";

            if (m_input_Account != null)
            {
                m_input_Account.text = _playerName;
            }

            return _playerName;
        }

        #endregion

        #region 场景加载

        private async UniTaskVoid LoadGameScene()
        {
            Log.Info("[LoginWindow] 进入游戏...");
            Log.Info($"[LoginWindow] 玩家ID: {Game.GameData.PlayerId}");
            Log.Info($"[LoginWindow] 服务器: {Game.GameData.ServerAddress}:{Game.GameData.ServerPort}");

            GameModule.UI.CloseUI<LoginWindow>();
            await GameModule.Scene.LoadSceneAsync("Game");
            await GameModule.TPBattleContext.InitializeGameScene();

            Log.Info("[LoginWindow] 进入游戏完成");
        }

        #endregion
    }
}
