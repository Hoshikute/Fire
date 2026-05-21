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

            // 加载 Game 场景
            await GameModule.Scene.LoadSceneAsync("Game");

            // 通过 TPBattleContext 初始化场景（相机设置 → Player 动态加载）
            await GameModule.TPBattleContext.InitializeGameScene();
        }
    }
}
