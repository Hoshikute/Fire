using Cysharp.Threading.Tasks;
using GameLogic.Character;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Game 世界控制器。
    /// 负责管理 Game 场景的初始化顺序：相机设置 → Player 动态加载。
    /// </summary>
    public class TPBattleContext : Module, ITPBattleContext
    {
        private const string PLAYER_PREFAB_PATH = "Player";

        public bool IsInitialized { get; private set; }

        public override void OnInit()
        {
            IsInitialized = false;
            Log.Info("[TPBattleContext] Initialized.");
        }

        public override void Shutdown()
        {
            IsInitialized = false;
            Log.Info("[TPBattleContext] Shutdown.");
        }

        /// <summary>
        /// 初始化 Game 场景，按顺序执行：相机设置 → Player 加载。
        /// </summary>
        public async UniTask InitializeGameScene()
        {
            Log.Info("[TPBattleContext] Begin InitializeGameScene...");

            // 步骤1：设置主相机（Main Camera 已在场景中）
            SetupMainCamera();

            // 步骤2：动态加载 Player
            await LoadPlayerAsync();

            IsInitialized = true;
            Log.Info("[TPBattleContext] Game scene initialized successfully.");
        }

        private void SetupMainCamera()
        {
            var mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
            foreach (var go in mainCameras)
            {
                var cam = go.GetComponent<Camera>();
                if (cam != null && cam.enabled)
                {
                    GameModule.Camera.SetMainCamera(cam);
                    Log.Info($"[TPBattleContext] Main camera set: {cam.name}");
                    return;
                }
            }

            Log.Error("[TPBattleContext] No enabled MainCamera found in scene.");
        }

        private async UniTask LoadPlayerAsync()
        {
            // 配置 Player prefab 路径
            GameModule.Character.SetCharacterPrefab(PLAYER_PREFAB_PATH);

            // 动态加载 Player
            var player = await GameModule.Character.LoadCharacterAsync();

            if (player != null)
            {
                Log.Info($"[TPBattleContext] Player loaded successfully: {player.name}");

                // 注入配置到 PlayerFrameSyncEntry
                var entry = player.GetComponent<PlayerFrameSyncEntry>();
                if (entry == null)
                {
                    entry = player.AddComponent<PlayerFrameSyncEntry>();
                }

                VerifyGroundLayer(player);
            }
            else
            {
                Log.Error("[TPBattleContext] Failed to load Player.");
            }
        }

        /// <summary>
        /// 诊断：验证 Player 的 ground LayerMask 与场景地面 Layer 是否匹配。
        /// </summary>
        private void VerifyGroundLayer(GameObject playerGo)
        {
            var entry = playerGo.GetComponent<PlayerFrameSyncEntry>();
            if (entry == null)
            {
                Log.Warning("[TPC_DIAG] PlayerFrameSyncEntry not found on loaded prefab - ground layer check skipped.");
                return;
            }

            // FrameSync 架构下地面逻辑由 IDeterministicGround 管理，不再依赖 whatIsGround LayerMask
            Log.Info("[TPC_DIAG] FrameSync player loaded — ground handling via IDeterministicGround.");
        }
    }
}
