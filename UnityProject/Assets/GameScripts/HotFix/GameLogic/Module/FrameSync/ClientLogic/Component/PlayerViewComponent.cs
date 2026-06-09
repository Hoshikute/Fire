// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

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

        /// <summary>是否已完成首帧对齐（避免第一帧从原点飞过来）。</summary>
        public bool initialized;

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
    }
}
