using Cysharp.Threading.Tasks;
using GameLogic.Character;
using Animancer;
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
        private const string PLAYER_ANIM_CONFIG_PATH = "PlayerAnimConfig";
        private const string LOCAL_PLAYER_ENTITY_KEY = "LocalPlayer";
        private const string TraceHeader = "[TPBattleContext]";

        private readonly Vector3 _playerSpawnPos = Vector3.zero;
        private WorldBase        m_playerWorld;
        private int              m_playerEntityId;
        private GameObject       m_playerInstance;
        private PlayerAnimConfig m_playerAnimConfig;

        public bool IsInitialized { get; private set; }

        public override void OnInit()
        {
            IsInitialized = false;
            Log.Info($"{TraceHeader} Initialized.");
        }

        public override void Shutdown()
        {
            CleanupPlayerFrameSync();
            if (m_playerInstance != null)
            {
                GameModule.Character.DestroyCharacter();
            }

            m_playerInstance = null;
            m_playerAnimConfig = null;
            IsInitialized = false;
            Log.Info($"{TraceHeader} Shutdown.");
        }

        /// <summary>
        /// 初始化 Game 场景，按顺序执行：相机设置 → Player 加载。
        /// </summary>
        public async UniTask InitializeGameScene()
        {
            if (IsInitialized)
            {
                Log.Warning($"{TraceHeader} InitializeGameScene skipped: already initialized.");
                return;
            }

            Log.Info($"{TraceHeader} Begin InitializeGameScene...");

            // 步骤1：设置主相机（Main Camera 已在场景中）
            SetupMainCamera();

            // 步骤2：动态加载 Player
            await LoadPlayerAsync();

            IsInitialized = true;
            Log.Info($"{TraceHeader} Game scene initialized successfully.");
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
                    Log.Info($"{TraceHeader} Main camera set: {cam.name}");
                    return;
                }
            }

            Log.Error($"{TraceHeader} No enabled MainCamera found in scene.");
        }

        private async UniTask LoadPlayerAsync()
        {
            // 配置 Player prefab 路径
            GameModule.Character.SetCharacterPrefab(PLAYER_PREFAB_PATH);

            // 动态加载 Player
            var player = await GameModule.Character.LoadCharacterAsync();

            if (player != null)
            {
                m_playerInstance = player;
                Log.Info($"{TraceHeader} Player loaded successfully: {player.name}");

                m_playerAnimConfig = await LoadPlayerAnimConfigAsync();
                StartPlayerFrameSync(player);
                LogGroundHandlingMode();
            }
            else
            {
                Log.Error($"{TraceHeader} Failed to load Player.");
            }
        }

        private async UniTask<PlayerAnimConfig> LoadPlayerAnimConfigAsync()
        {
            if (m_playerAnimConfig != null)
            {
                return m_playerAnimConfig;
            }

            if (!GameModule.Resource.CheckLocationValid(PLAYER_ANIM_CONFIG_PATH))
            {
                Log.Error($"{TraceHeader} PlayerAnimConfig missing. Expected resource location: {PLAYER_ANIM_CONFIG_PATH}. Animation playback cannot start.");
                return null;
            }

            PlayerAnimConfig config = await GameModule.Resource.LoadAssetAsync<PlayerAnimConfig>(PLAYER_ANIM_CONFIG_PATH);
            if (config == null)
            {
                Log.Error($"{TraceHeader} PlayerAnimConfig load returned null. Resource location: {PLAYER_ANIM_CONFIG_PATH}.");
                return null;
            }

            string missingFields;
            if (!config.HasRequiredBaseTransitions(out missingFields))
            {
                Log.Error($"{TraceHeader} PlayerAnimConfig invalid at {PLAYER_ANIM_CONFIG_PATH}. Missing required transition fields: {missingFields}.");
                return null;
            }

            Log.Info($"{TraceHeader} PlayerAnimConfig loaded and validated: {PLAYER_ANIM_CONFIG_PATH}.");

            return config;
        }

        private void StartPlayerFrameSync(GameObject player)
        {
            if (m_playerWorld != null)
            {
                Log.Warning($"{TraceHeader} PlayerWorld already exists, skip duplicate startup.");
                return;
            }

            AnimancerComponent animancer = player.GetComponent<AnimancerComponent>();
            if (animancer == null)
            {
                Log.Error($"{TraceHeader} AnimancerComponent missing on Player, PlayerWorld startup aborted.");
                return;
            }

            m_playerWorld = GameModule.FrameSync.CreateWorld<PlayerWorld>();
            m_playerWorld.SyncRule = SyncRule.Frame;

            Log.Info($"{TraceHeader} playerSpawnPos = {_playerSpawnPos}, fixed = {SyncVector3.FromVector3(_playerSpawnPos)}");
            SpawnLocalPlayerEntity(player.transform, animancer, m_playerAnimConfig);

            // 立即提交实体，避免首帧系统因 LazyExecuteEntityOperation 尚未执行而读不到玩家。
            m_playerWorld.FlushEntityOperations();
            m_playerWorld.IsStart = true;

            Log.Info($"{TraceHeader} PlayerWorld started, logic frame step {GameModule.FrameSync.IntervalTime}ms.");
            TryBindCamera(player.transform);
        }

        private void SpawnLocalPlayerEntity(Transform playerRoot, AnimancerComponent animancer, PlayerAnimConfig animConfig)
        {
            SyncVector3 rawPos = SyncVector3.FromVector3(_playerSpawnPos);
            IDeterministicGround ground = new FlatGround(0);
            int groundY = ground.SampleHeight(rawPos.x, rawPos.z);
            int tolerance = 100;

            bool isOnGround;
            SyncVector3 pos;
            if (rawPos.y <= groundY + tolerance)
            {
                pos = SyncVector3.FromRaw(rawPos.x, groundY, rawPos.z);
                isOnGround = true;
            }
            else
            {
                pos = rawPos;
                isOnGround = false;
                Log.Warning($"{TraceHeader} _playerSpawnPos.y={_playerSpawnPos.y} 在地面以上 {_playerSpawnPos.y - groundY / 1000f:F2}m，" +
                            "将以非接地状态生成（会触发自由落体）。");
            }

            PlayerMoveComponent move = new PlayerMoveComponent
            {
                pos        = pos,
                faceDir    = SyncVector3.FromRaw(0, 0, SyncVector3.ONE),
                isOnGround = isOnGround,
            };

            PlayerStateComponent state = new PlayerStateComponent
            {
                state         = PlayerLogicState.Idle,
                framesInState = 0,
            };

            PlayerViewComponent view = new PlayerViewComponent
            {
                viewRoot   = playerRoot,
                animancer  = animancer,
                animConfig = animConfig,
            };

            m_playerEntityId = LOCAL_PLAYER_ENTITY_KEY.ToHash();
            m_playerWorld.CreateEntity(m_playerEntityId, move, state, view);
        }

        private void TryBindCamera(Transform playerRoot)
        {
            Transform anchor = ResolveFollowAnchor(playerRoot);
            if (anchor == null)
            {
                Log.Error($"{TraceHeader} Unable to resolve camera anchor, camera bind skipped.");
                return;
            }

            Log.Info($"{TraceHeader} Bind camera: follow/lookAt = {anchor.name}");
            GameModule.Camera.BindCinemachineToPlayer(anchor, anchor);
        }

        private Transform ResolveFollowAnchor(Transform playerRoot)
        {
            Transform child = playerRoot.Find("LookAt");
            if (child != null)
            {
                Log.Info($"{TraceHeader}[ResolveFollowAnchor] Found child LookAt: {child.name}");
                return child;
            }

            Log.Warning($"{TraceHeader}[ResolveFollowAnchor] LookAt child not found, fallback to player root.");
            return playerRoot;
        }

        private void CleanupPlayerFrameSync()
        {
            if (m_playerWorld == null)
            {
                m_playerEntityId = 0;
                return;
            }

            GameModule.FrameSync.DestroyWorld(m_playerWorld);
            m_playerWorld = null;
            m_playerEntityId = 0;
        }

        /// <summary>
        /// 诊断：说明 FrameSync 角色的地面处理模式。
        /// </summary>
        private void LogGroundHandlingMode()
        {
            // FrameSync 架构下地面逻辑由 IDeterministicGround 管理，不再依赖 whatIsGround LayerMask
            Log.Info("[TPC_DIAG] FrameSync player loaded — ground handling via IDeterministicGround.");
        }
    }
}
