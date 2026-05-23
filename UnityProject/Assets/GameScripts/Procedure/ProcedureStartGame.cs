using Cysharp.Threading.Tasks;
using Launcher;
using TEngine;

namespace Procedure
{
    public class ProcedureStartGame : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        protected override void OnEnter(IFsm<IProcedureModule> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            StartGame().Forget();
        }

        private async UniTaskVoid StartGame()
        {
            await UniTask.Yield();
            LauncherMgr.HideAllUI();

            // 场景加载前清理多余相机
            GameModule.Camera.CleanupExtraCameras();

            // Game 场景由 LoginUI 点击服务器后加载
            // LoginUI.LoadGameScene() 负责场景加载和初始化
        }
    }
}
