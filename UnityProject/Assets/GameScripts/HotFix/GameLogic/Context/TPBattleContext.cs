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
            GameModule.Character.SetThirdPersonPlayerPrefab(PLAYER_PREFAB_PATH);

            // 动态加载 Player
            var player = await GameModule.Character.LoadThirdPersonPlayerAsync();

            if (player != null)
            {
                Log.Info($"[TPBattleContext] Player loaded successfully: {player.name}");
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
            var playerComp = playerGo.GetComponent<ThirdPersonController.Player>();
            if (playerComp == null)
            {
                Log.Error("[TPC_DIAG] Player component not found on loaded prefab!");
                return;
            }

            var groundMask = playerComp.whatIsGround;

            // 将 LayerMask 值解析为 layer 名称
            var layerNames = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 32; i++)
            {
                if ((groundMask.value & (1 << i)) != 0)
                {
                    layerNames.Add($"{i}:{LayerMask.LayerToName(i)}");
                }
            }
            Log.Info($"[TPC_DIAG] Player whatIsGround Mask = {groundMask.value} → [{string.Join(", ", layerNames)}]");

            // 检测场景中所有带有 "Ground" 或 "Terrain" 标记的对象
            var allObjects = Object.FindObjectsOfType<GameObject>();
            var groundLayerObjects = new System.Collections.Generic.List<string>();
            foreach (var go in allObjects)
            {
                int layer = go.layer;
                string layerName = LayerMask.LayerToName(layer);
                if (layerName.Contains("Ground") || layerName.Contains("Terrain") || layerName.Contains("ground"))
                {
                    if (!groundLayerObjects.Contains(layerName))
                        groundLayerObjects.Add(layerName);
                }
            }
            if (groundLayerObjects.Count > 0)
            {
                Log.Info($"[TPC_DIAG] Scene ground/terrain layers found: [{string.Join(", ", groundLayerObjects)}]");
            }
            else
            {
                Log.Warning("[TPC_DIAG] No objects with 'Ground'/'Terrain' layer found in scene! This may cause ground detection failures.");
            }

            // 检查 Mask 是否覆盖了 Default layer (0) — 通常地面在 Default
            bool masksDefault = (groundMask.value & 1) != 0;
            Log.Info($"[TPC_DIAG] whatIsGround includes Default(0)? {masksDefault}");
        }
    }
}
