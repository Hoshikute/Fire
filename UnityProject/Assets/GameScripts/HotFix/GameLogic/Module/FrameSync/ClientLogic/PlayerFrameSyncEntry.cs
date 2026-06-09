// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 帧同步角色控制入口（表现层 MonoBehaviour）。
    /// 挂在场景里，负责：
    ///   1. 通过 GameModule.FrameSync 创建 PlayerWorld；
    ///   2. 生成一个玩家逻辑实体（PlayerMoveComponent 逻辑 + PlayerViewComponent 绑定表现 Transform）；
    ///   3. 启动世界（IsStart=true），之后由 FrameSyncModule 主循环驱动逻辑帧 / 渲染帧。
    ///
    /// 这是「传统单机控制器」与「FrameSync ECS」的桥：
    /// 老的 ThirdPersonController 仍可独立存在；本入口提供一条用帧同步 ECS 跑角色移动的最小路径。
    /// 把本组件挂到一个空 GameObject，并把要被驱动的角色 Transform 拖到 viewRoot 即可运行。
    /// </summary>
    public class PlayerFrameSyncEntry : MonoBehaviour
    {
        [Header("被帧同步逻辑驱动的角色表现根（Transform）")]
        [SerializeField] private Transform _viewRoot;

        [Header("初始逻辑位置（米，会转成定点数）")]
        [SerializeField] private Vector3 _spawnPos = Vector3.zero;

        private WorldBase m_world;
        private int m_playerEntityId;

        private void Start()
        {
            if (_viewRoot == null)
            {
                _viewRoot = transform;
                Log.Warning("[PlayerFrameSyncEntry] 未指定 viewRoot，默认用自身 Transform。");
            }

            // 1) 创建世界（CreateWorld 内部会 Init 并加入 FrameSyncModule 的世界列表）。
            m_world = GameModule.FrameSync.CreateWorld<PlayerWorld>();
            m_world.SyncRule = SyncRule.Frame; // 帧同步规则：本地算结果，只同步输入

            // 2) 生成玩家实体。逻辑组件 + 表现组件一并挂上。
            SpawnPlayer();

            // 3) 启动世界：之后 FrameSyncModule.Update 会驱动 Loop/FixedLoop。
            m_world.IsStart = true;

            Log.Info($"[PlayerFrameSyncEntry] PlayerWorld 已启动，逻辑帧步长 {GameModule.FrameSync.IntervalTime}ms。");
        }

        private void SpawnPlayer()
        {
            // 逻辑组件（确定性，可回滚）
            PlayerMoveComponent move = new PlayerMoveComponent
            {
                pos = SyncVector3.FromVector3(_spawnPos),
                faceDir = SyncVector3.FromRaw(0, 0, SyncVector3.ONE),
                isOnGround = true,
            };

            // 表现组件（绑定 Unity Transform，仅表现层用）
            PlayerViewComponent view = new PlayerViewComponent
            {
                viewRoot = _viewRoot,
            };

            // 稳定实体 ID：用固定标识，保证跨端 / 回滚一致。
            m_playerEntityId = "LocalPlayer".ToHash();
            m_world.CreateEntity(m_playerEntityId, move, view);

            // CreateEntity 进的是 createCache，会在下一个 FixedLoop 的 LazyExecuteEntityOperation 真正加入世界。
        }

        private void OnDestroy()
        {
            if (m_world != null && GameModule.FrameSync != null)
            {
                GameModule.FrameSync.DestroyWorld(m_world);
                m_world = null;
            }
        }
    }
}
