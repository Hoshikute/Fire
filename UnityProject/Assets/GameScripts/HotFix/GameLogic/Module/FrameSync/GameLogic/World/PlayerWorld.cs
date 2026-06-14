
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
    ///                              PlayerInputCommandSystem ──▶ PlayerCommandRecordComponent(按实体帧指令)
    ///                                   │
    ///                                   ▼
    ///                              PlayerMoveSystem ──▶ PlayerMoveComponent(可回滚)
    ///                                                   │
    ///   PlayerViewSystem  ◀────────────────────────────┘ (只读)
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
                typeof(PlayerInputCollectSystem), // 表现层：渲染帧采集 Unity 输入（含相机修正）→ 单例
                typeof(PlayerInputCommandSystem), // 逻辑层：本地输入固化为本地实体帧指令，非本地实体按缓存/预测命令
                typeof(PlayerMoveSystem),         // 逻辑层：按实体帧指令确定性移动（走/跑/跳/重力/空中惯性）
                typeof(PlayerStateSystem),        // 逻辑层：按实体帧指令+物理事实推导状态（排在 Move 之后）
                typeof(PlayerViewSystem),         // 表现层：渲染帧读 pos/faceDir 驱动 Transform 插值
                typeof(PlayerAnimViewSystem),     // 表现层：渲染帧读 State/Move/View 驱动 Animancer
            };
        }

        public override Type[] GetRecordTypes()
        {
            // 声明可回滚组件：World 会用 RecordSystem<T> 自动每帧快照。
            return new Type[]
            {
                typeof(PlayerMoveComponent),
                typeof(PlayerStateComponent),
            };
        }
    }
}
