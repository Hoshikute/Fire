// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家表现组件（仅表现层使用，不参与回滚 / 网络同步）。
    /// 持有逻辑实体对应的 Unity 表现对象引用：Transform（位置朝向）与可选的 Animancer 宿主。
    /// 表现层据此把确定性逻辑状态「渲染」出来。
    ///
    /// 注意：它继承普通 ComponentBase（非 MomentComponentBase），因为引用了 UnityEngine 对象，
    /// 绝不能进入逻辑层快照 / 网络协议。逻辑与表现的边界就在这里。
    /// </summary>
    public class PlayerViewComponent : ComponentBase
    {
        /// <summary>表现用的根 Transform（被插值驱动）。</summary>
        public Transform viewRoot;

        /// <summary>上一次渲染所用的逻辑位置（用于插值起点），定点。</summary>
        public SyncVector3 lastLogicPos = SyncVector3.Zero;

        /// <summary>是否已完成首帧对齐（避免第一帧从原点插值过来）。</summary>
        public bool initialized;
    }
}
