using TEngine;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 登录窗口 - 账号输入与登录
    /// </summary>
    [Window(UILayer.UI, "LoginWindow")]
    public class LoginWindow : UIWindow
    {
        #region UI 组件

        // 标题文本
        private Text m_text_Title;

        // 账号输入框
        private InputField m_input_Account;

        // 密码输入框（预留）
        private InputField m_input_Password;

        // 随机名字按钮
        private Button m_btn_RandomName;

        // 登录按钮
        private Button m_btn_Login;

        #endregion

        #region 状态

        private string _playerName;

        // 随机名字库
        private static readonly string[] s_firstNames = { "勇敢的", "聪明的", "快速的", "强大的", "神秘的", "传说中的", "无敌的", "闪耀的" };
        private static readonly string[] s_lastNames = { "战士", "法师", "弓箭手", "骑士", "刺客", "牧师", "术士", "武僧" };

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
            // 显示鼠标
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 生成随机名字
            GenerateRandomName();

            Log.Info("[LoginWindow] 登录窗口创建完成");
        }

        protected override void OnRefresh()
        {
            // 刷新显示（如有需要）
        }

        protected override void OnDestroy()
        {
            // 清理资源
        }

        #endregion

        #region 按钮事件

        private void OnRandomNameClick()
        {
            GenerateRandomName();
            Log.Info($"[LoginWindow] 生成随机名字: {_playerName}");
        }

        private void OnLoginClick()
        {
            Log.Info("[LoginWindow] 点击进入游戏");

            // 获取玩家名称
            _playerName = m_input_Account?.text;
            if (string.IsNullOrEmpty(_playerName))
            {
                _playerName = GenerateRandomName();
                if (m_input_Account != null)
                {
                    m_input_Account.text = _playerName;
                }
            }

            // 加载游戏场景
            LoadGameScene().Forget();
        }

        #endregion

        #region 随机名字

        private string GenerateRandomName()
        {
            string firstName = s_firstNames[Random.Range(0, s_firstNames.Length)];
            string lastName = s_lastNames[Random.Range(0, s_lastNames.Length)];
            string number = Random.Range(100, 999).ToString();

            _playerName = $"{firstName}{lastName}{number}";

            // 更新输入框显示
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

            // 保存玩家名称
            Game.GameData.PlayerName = _playerName;

            Log.Info($"[LoginWindow] 玩家: {_playerName}");
            Log.Info($"[LoginWindow] 服务器: {Game.GameData.ServerAddress}:{Game.GameData.ServerPort}");

            // 关闭登录界面
            GameModule.UI.CloseUI<LoginWindow>();

            // 加载 Game 场景
            await GameModule.Scene.LoadSceneAsync("Game");

            // 初始化 Game 场景（设置相机 + 加载 Player）
            await GameModule.TPBattleContext.InitializeGameScene();

            Log.Info("[LoginWindow] 进入游戏完成");
        }

        #endregion
    }
}
