// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using Animancer;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 帧同步角色控制入口（表现层 MonoBehaviour）。
    /// 完整替代原 ThirdPersonController.Player：
    ///   - 不再依赖 ThirdPersonController 命名空间的任何类型。
    ///   - 负责创建 PlayerWorld、生成玩家逻辑实体、绑定 Animancer / 相机。
    ///   - 摄像机绑定逻辑从 Player.cs 迁移到此处（Start 延迟绑定，保证 CameraModule 已初始化）。
    ///
    /// 使用方式：
    ///   1. 把本组件挂到场景里的角色根 GameObject（需挂有 AnimancerComponent）。
    ///   2. 把动画配置 SO（PlayerAnimConfig）拖入 animConfig 字段。
    ///   3. 如需手动指定相机锚点，把子物体 LookAt Transform 拖入 lookAtTarget；
    ///      否则自动查找名为 "LookAt" 的子节点，再回退到根节点。
    ///   4. 启动后由 FrameSyncModule 主循环驱动逻辑帧 / 渲染帧。
    /// </summary>
    public class PlayerFrameSyncEntry : MonoBehaviour
    {
        private const string TraceHeader = "[PlayerFrameSyncEntry]";

        [Header("动画")]
        [Tooltip("动画剪辑配置 ScriptableObject（PlayerAnimConfig）")]
        [SerializeField] private PlayerAnimConfig _animConfig;

        [Header("相机")]
        [Tooltip("相机 LookAt 锚点（留空时自动查找子节点 LookAt，再回退到根节点）")]
        [SerializeField] private Transform _lookAtTarget;

        [Header("初始逻辑位置（米，转为定点数）")]
        [SerializeField] private Vector3 _spawnPos = Vector3.zero;

        // ── 运行时 ──────────────────────────────────────────────────────
        private WorldBase          m_world;
        private int                m_playerEntityId;
        private AnimancerComponent m_animancer;
        private bool               m_pendingCameraBind;

        // ── Unity 生命周期 ───────────────────────────────────────────────

        private void Awake()
        {
            m_animancer = GetComponent<AnimancerComponent>();
            if (m_animancer == null)
            {
                Log.Error($"{TraceHeader} 未找到 AnimancerComponent，请检查挂载对象。");
            }

            if (_animConfig == null)
            {
                Log.Warning($"{TraceHeader} animConfig 未赋值，动画将无法播放。");
            }

            m_pendingCameraBind = true;
        }

        private void Start()
        {
            // 1) 创建世界
            m_world = GameModule.FrameSync.CreateWorld<PlayerWorld>();
            m_world.SyncRule = SyncRule.Frame;

            // 2) 生成玩家实体
            Log.Info($"{TraceHeader} _spawnPos = {_spawnPos}, 转定点: {SyncVector3.FromVector3(_spawnPos)}");
            SpawnPlayer();

            // 2.5) 立即提交实体（避免首帧丢失：LazyExecuteEntityOperation 在 FixedLoop 末尾才执行）
            m_world.FlushEntityOperations();

            // 3) 启动世界
            m_world.IsStart = true;

            Log.Info($"{TraceHeader} PlayerWorld 已启动，逻辑帧步长 {GameModule.FrameSync.IntervalTime}ms。");

            // 4) 延迟绑定相机（CameraModule 在 Start 阶段才保证初始化完成）
            if (m_pendingCameraBind)
            {
                TryBindCamera();
            }
        }

        private void OnDestroy()
        {
            if (m_world != null && GameModule.FrameSync != null)
            {
                GameModule.FrameSync.DestroyWorld(m_world);
                m_world = null;
            }
        }

        // ── 实体生成 ─────────────────────────────────────────────────────

        private void SpawnPlayer()
        {
            // 用地面查询修正 spawn y 并确定初始接地状态
            SyncVector3 rawPos = SyncVector3.FromVector3(_spawnPos);
            IDeterministicGround ground = new FlatGround(0);
            int groundY = ground.SampleHeight(rawPos.x, rawPos.z);
            int tolerance = 100; // 微小容差（≈0.1m）

            bool isOnGround;
            SyncVector3 pos;
            if (rawPos.y <= groundY + tolerance)
            {
                // 位置在地面或以下：吸附到地面
                pos = SyncVector3.FromRaw(rawPos.x, groundY, rawPos.z);
                isOnGround = true;
            }
            else
            {
                // 位置在空中：保留原始 y，标记不接地
                pos = rawPos;
                isOnGround = false;
                Log.Warning($"{TraceHeader} _spawnPos.y={_spawnPos.y} 在地面以上 {_spawnPos.y - groundY / 1000f:F2}m，" +
                            "将以非接地状态生成（会触发自由落体）。");
            }

            // 逻辑组件（确定性，可回滚）
            PlayerMoveComponent move = new PlayerMoveComponent
            {
                pos        = pos,
                faceDir    = SyncVector3.FromRaw(0, 0, SyncVector3.ONE),
                isOnGround = isOnGround,
            };

            // 逻辑状态组件（确定性，可回滚）
            PlayerStateComponent state = new PlayerStateComponent
            {
                state         = PlayerLogicState.Idle,
                framesInState = 0,
            };

            // 表现组件（绑定 Unity 对象，不进快照）
            PlayerViewComponent view = new PlayerViewComponent
            {
                viewRoot   = transform,
                animancer  = m_animancer,
                animConfig = _animConfig,
            };

            // 稳定实体 ID（跨端 / 回滚一致）
            m_playerEntityId = "LocalPlayer".ToHash();
            m_world.CreateEntity(m_playerEntityId, move, state, view);
        }

        // ── 相机绑定（从原 Player.cs 迁移）──────────────────────────────

        private void TryBindCamera()
        {
            Transform anchor = ResolveFollowAnchor();
            if (anchor == null)
            {
                Log.Error($"{TraceHeader} 无法确定相机锚点，相机绑定跳过。");
                return;
            }

            Log.Info($"{TraceHeader} 绑定相机：follow/lookAt = {anchor.name}");
            GameModule.Camera.BindCinemachineToPlayer(anchor, anchor);
            m_pendingCameraBind = false;
        }

        /// <summary>
        /// 解析相机 Follow / LookAt 锚点。
        /// 优先级：序列化字段 _lookAtTarget → 子节点 "LookAt" → 自身根节点。
        /// </summary>
        private Transform ResolveFollowAnchor()
        {
            if (_lookAtTarget != null)
            {
                Log.Info($"{TraceHeader}[ResolveFollowAnchor] 使用序列化 LookAt 目标：{_lookAtTarget.name}");
                return _lookAtTarget;
            }

            Transform child = transform.Find("LookAt");
            if (child != null)
            {
                _lookAtTarget = child;
                Log.Info($"{TraceHeader}[ResolveFollowAnchor] 自动找到子节点 LookAt：{child.name}");
                return child;
            }

            Log.Warning($"{TraceHeader}[ResolveFollowAnchor] 未找到 LookAt 锚点，回退到角色根节点。");
            return transform;
        }

        // ── 运行时注入（供 TPBattleContext 等业务入口调用）──────────

        /// <summary>
        /// 运行时注入动画配置（替代 Inspector 序列化赋值）。
        /// 必须在 Start() 之前调用，否则已启动的 World 不会感知新的配置。
        /// </summary>
        public void SetAnimConfig(PlayerAnimConfig config)
        {
            _animConfig = config;
        }

        /// <summary>
        /// 运行时注入相机 LookAt 锚点（替代 Inspector 序列化赋值）。
        /// 如果在 Start() 之后调用，需要手动重新绑定相机。
        /// </summary>
        public void SetLookAtTarget(Transform target)
        {
            _lookAtTarget = target;
        }
    }
}
