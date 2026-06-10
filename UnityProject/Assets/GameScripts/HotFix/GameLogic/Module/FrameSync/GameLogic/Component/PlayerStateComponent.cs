// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

namespace GameLogic
{
    /// <summary>
    /// 玩家逻辑状态枚举（确定性）。
    /// 对齐原 ThirdPersonController 的完整状态树，仅保留逻辑层需要区分的状态；
    /// 表现细节（idle 轮播、混合树过渡）留给表现层 PlayerAnimViewSystem 驱动，不进逻辑。
    ///
    /// 枚举值固定不变更（协议序列化 / 快照依赖），新增状态追加到末尾。
    /// </summary>
    public enum PlayerLogicState
    {
        // ── 地面常态 ──────────────────────────────────────────────────
        /// <summary>站立待机（无移动输入，接地）。</summary>
        Idle = 0,

        /// <summary>移动启步（刚开始移动的起步帧，接地有输入）。</summary>
        MoveStart = 1,

        /// <summary>移动循环（持续移动中，接地有输入）。</summary>
        MoveLoop = 2,

        /// <summary>移动结束（刚停止移动的收步帧，接地无输入）。</summary>
        MoveEnd = 3,

        // ── 空中 ──────────────────────────────────────────────────────
        /// <summary>跳跃上升段（verticalSpeed > 0，离地）。</summary>
        Jump = 4,

        /// <summary>就地跳跃（起跳时无移动输入）。对应老 OutPlaceJump。</summary>
        JumpInPlace = 5,

        /// <summary>下落段（verticalSpeed ≤ 0，离地）。</summary>
        Fall = 6,

        /// <summary>落地缓冲（着地后短暂恢复帧）。</summary>
        Land = 7,

        // ── 锁定模式 ──────────────────────────────────────────────────
        /// <summary>锁定待机（开启锁敌模式，无移动输入）。</summary>
        LockIdle = 8,

        // ── 交互 / 攀爬 ───────────────────────────────────────────────
        /// <summary>靠近墙壁（检测到前方墙体，准备挂壁/攀爬）。</summary>
        MoveToWall = 9,

        /// <summary>挂墙 / Vault（矮障碍物翻越）。</summary>
        Vault = 10,

        /// <summary>攀爬（高障碍物 Climb）。</summary>
        Climb = 11,

        /// <summary>边缘攀上（LedgeClimb，挂檐后拉上去）。</summary>
        LedgeClimb = 12,

        /// <summary>平台跳（PlatformerUp，从低处跳跃到平台）。</summary>
        PlatformerUp = 13,
    }

    /// <summary>
    /// 玩家逻辑状态组件（可回滚的「时刻组件」）。
    /// 持有确定性的状态枚举 + 进入当前状态后经过的逻辑帧数（替代老 TPC 的 GameModule.Timer）。
    ///
    /// 为什么用帧计数而非真实时间：帧同步逻辑层禁止 Time / Timer（非确定、不可回滚）。
    /// 「落地后 0.05s 检测下落」等老逻辑改成「经过 N 个逻辑帧」，N = 时长 / 200ms，跨端一致。
    ///
    /// 参与预测回滚，必须 DeepCopy 全字段（全为值类型，直接赋值即可）。
    /// </summary>
    public class PlayerStateComponent : MomentComponentBase
    {
        /// <summary>当前逻辑状态。</summary>
        public PlayerLogicState state = PlayerLogicState.Idle;

        /// <summary>进入当前状态后经过的逻辑帧数（每个 FixedUpdate +1）。</summary>
        public int framesInState = 0;

        /// <summary>
        /// 上一帧的逻辑状态。用于表现层检测「状态刚切换」，驱动动画过渡。
        /// 逻辑层本身不使用此字段。
        /// </summary>
        public PlayerLogicState prevState = PlayerLogicState.Idle;

        /// <summary>
        /// 是否处于锁定模式（锁敌模式）。
        /// 由表现层的锁定输入写入（通过 PlayerInputComponent.toggleLock），
        /// 影响 LockIdle 状态决策。
        /// </summary>
        public bool isLocked = false;

        /// <summary>
        /// 是否有平台跳请求（PlatformerUp）。
        /// 由交互层写入，PlayerStateSystem 在地面状态下消费并切换到 PlatformerUp。
        /// </summary>
        public bool platformJumpRequested = false;

        /// <summary>
        /// 当前检测到的障碍物类型（用于决策 Vault vs Climb vs LedgeClimb）。
        /// 由碰撞检测系统写入（逻辑层），PlayerStateSystem 读取。
        /// 0 = 无障碍，1 = Vault，2 = Climb，3 = LedgeClimb。
        /// </summary>
        public int wallObstructType = 0;

        /// <summary>
        /// 是否为原地跳跃（起跳时无移动输入）。
        /// 由 PlayerMoveSystem 在触发跳跃时写入，PlayerStateSystem 据此选择 Jump vs JumpInPlace。
        /// </summary>
        public bool isInPlaceJump = false;

        public override MomentComponentBase DeepCopy()
        {
            PlayerStateComponent c = new PlayerStateComponent();
            c.ID = ID;
            c.Frame = Frame;
            c.state = state;
            c.framesInState = framesInState;
            c.prevState = prevState;
            c.isLocked = isLocked;
            c.platformJumpRequested = platformJumpRequested;
            c.wallObstructType = wallObstructType;
            c.isInPlaceJump = isInPlaceJump;
            return c;
        }
    }
}
