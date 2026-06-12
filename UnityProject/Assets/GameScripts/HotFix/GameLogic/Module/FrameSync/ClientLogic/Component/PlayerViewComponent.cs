
using Animancer;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家表现组件（仅表现层使用，不参与回滚 / 网络同步）。
    /// 持有逻辑实体对应的 Unity 表现对象引用：
    ///   - viewRoot：Transform（位置 / 朝向插值目标）
    ///   - animancer：Animancer 动画播放器（PlayerAnimViewSystem 驱动）
    ///   - animConfig：动画剪辑配置（从 PlayerAnimConfig SO 注入）
    ///
    /// 注意：继承普通 ComponentBase（非 MomentComponentBase），因为引用了 UnityEngine 对象，
    /// 绝不能进入逻辑层快照 / 网络协议。逻辑与表现的边界就在这里。
    /// </summary>
    public class PlayerViewComponent : ComponentBase
    {
        /// <summary>表现用的根 Transform（被插值驱动）。</summary>
        public Transform viewRoot;

        /// <summary>上一次渲染所用的逻辑位置（用于插值起点），定点。</summary>
        public SyncVector3 lastLogicPos = SyncVector3.Zero;

        /// <summary>
        /// 插值起点：上一逻辑帧的位置（定点）。
        /// PlayerViewSystem 检测到 pos 变化时，把旧值存入此字段，
        /// 然后用 Lerp(prevLogicPos → lastLogicPos, interpT) 在两个逻辑快照间线性过渡。
        /// </summary>
        public SyncVector3 prevLogicPos = SyncVector3.Zero;

        /// <summary>
        /// 插值进度（0→1），每个渲染帧 += dt / logicFrameDuration。
        /// ≥1 表示已追上最新逻辑位置。
        /// </summary>
        public float interpT = 1.1f;

        /// <summary>是否已完成首帧对齐（避免第一帧从原点飞过来）。</summary>
        public bool initialized;

        /// <summary>
        /// 动画是否已首次播放。
        /// PlayerAnimViewSystem 在首次执行时无条件播放当前状态动画，
        /// 之后仅在状态切换时触发播放。
        /// </summary>
        public bool animInitialized;

        /// <summary>
        /// Animancer 动画播放组件引用。
        /// 由 PlayerFrameSyncEntry 在 SpawnPlayer 时从角色 GameObject 上取得并注入。
        /// </summary>
        public AnimancerComponent animancer;

        /// <summary>
        /// 动画剪辑配置（ScriptableObject）。
        /// 由 PlayerFrameSyncEntry 序列化字段注入，解耦动画资源路径。
        /// </summary>
        public PlayerAnimConfig animConfig;

        /// <summary>
        /// PlatformerUp 当前阶段：0=start, 1=loop, 2=downLoop。
        /// 仅 PlayerAnimViewSystem 使用，不参与逻辑层。
        /// </summary>
        public int platformerUpPhase;
    }
}
