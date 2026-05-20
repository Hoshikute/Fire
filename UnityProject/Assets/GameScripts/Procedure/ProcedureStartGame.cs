using System;
using Cysharp.Threading.Tasks;
using Launcher;
using TEngine;
using UnityEngine;

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

            _ = GameModule.Character;
            await GameModule.Scene.LoadSceneAsync("Game");

            // 场景加载后初始化游戏相机
            SetupGameCamera();
        }

        private void SetupGameCamera()
        {
            var mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
            foreach (var go in mainCameras)
            {
                var cam = go.GetComponent<Camera>();
                if (cam != null && cam.enabled)
                {
                    GameModule.Camera.SetMainCamera(cam);
                    return;
                }
            }
        }
    }
}
