// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using System;

namespace GameLogic
{
    /// <summary>
    /// 玩家帧同步世界。
    /// 按 FrameSync 设计哲学组装一套「最小可跑」的角色控制 ECS：
    ///
    ///   表现层（渲染帧 Update）         逻辑层（逻辑帧 FixedUpdate, 200ms）
    ///   ─────────────────────         ──────────────────────────────
    ///   PlayerInputCollectSystem  ──▶  PlayerInputComponent(单例)
    ///                                   │
    ///                                   ▼
    ///                                  PlayerMoveSystem  ──▶  PlayerMoveComponent(可回滚)
    ///                                                          │
    ///   PlayerViewSystem  ◀──────────────────────────────────┘ (只读)
    ///
    /// System 注册顺序 = 调用顺序。采集系统排最前，保证逻辑帧推进前输入已就绪；
    /// 表现系统排最后，渲染最新逻辑状态。
    /// 可回滚组件通过 GetRecordTypes 声明，World 用 RecordSystem&lt;T&gt; 自动给它做快照（支持预测回滚）。
    /// </summary>
    public class PlayerWorld : WorldBase
    {
        public override Type[] GetSystemTypes()
        {
            return new Type[]
            {
                typeof(PlayerInputCollectSystem), // 表现层：渲染帧采集 Unity 输入 → 单例
                typeof(PlayerMoveSystem),         // 逻辑层：逻辑帧确定性移动
                typeof(PlayerViewSystem),         // 表现层：渲染帧读逻辑状态驱动 Transform
            };
        }

        public override Type[] GetRecordTypes()
        {
            // 声明可回滚组件：World 会用 RecordSystem<PlayerMoveComponent> 自动每帧快照。
            return new Type[]
            {
                typeof(PlayerMoveComponent),
            };
        }
    }
}
