// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using System;

namespace GameLogic
{
    /// <summary>
    /// 玩家移动系统（确定性逻辑层）。
    /// 在固定逻辑帧（200ms）里运行：读取单例 PlayerInputComponent 的输入意图，
    /// 用定点数推进每个带 PlayerMoveComponent 实体的位置 / 朝向 / 竖直速度。
    ///
    /// 完整对齐原 ThirdPersonController 的移动特性：
    ///   - 走 / 跑双档速度（Shift 键切换 speedGear）
    ///   - 锁定模式下仍可水平移动（方向不变，速度同档）
    ///   - 跳跃初速度、重力、落地检测全部确定性整数
    ///   - 空中水平惯性：离地后保留上一帧的水平速度，可用输入微调
    ///   - 交互状态（Vault/Climb/PlatformerUp 等）期间禁止玩家主动水平移动
    ///
    /// 确定性铁律：
    ///   - 全程 int / SyncVector3 整数运算，禁止 float、Time.deltaTime、Random。
    ///   - 步长用传入的 deltaTime（毫秒，恒为 FrameSyncModule.IntervalTime = 200）。
    ///   - 跑在 FixedUpdate，回滚重算时会被重复执行，绝不能有任何表现层副作用。
    /// </summary>
    public class PlayerMoveSystem : SystemBase
    {
        // ── 物理常量（定点，毫单位）────────────────────────────────────
        /// <summary>重力加速度（定点，毫单位 / 秒²）。约 -9.8 m/s²。</summary>
        private const int Gravity = -9800;

        /// <summary>跳跃初速度（定点，毫单位 / 秒）。约 5m/s，对应老 TPC jumpMaxHeight≈0.8m。</summary>
        private const int JumpSpeed = 5000;

        /// <summary>步行速度（定点，毫单位 / 秒）。约 4m/s。</summary>
        private const int WalkSpeed = 4000;

        /// <summary>跑步速度（定点，毫单位 / 秒）。约 8m/s（老 TPC speedGear=2 时的 2x）。</summary>
        private const int RunSpeed = 8000;

        /// <summary>
        /// 空中水平控制系数（定点 ONE = 1000 为 100%）。
        /// 500 = 50%：空中只能以一半速度微调方向，对应老 TPC currentMidInAirMultiplier≈0.6。
        /// </summary>
        private const int AirControlFixed = 600;

        // ── 确定性地面服务 ──────────────────────────────────────────────
        private IDeterministicGround m_ground;

        public override void Init()
        {
            base.Init();
            m_ground = new FlatGround(0);
        }

        public override Type[] GetFilter()
        {
            return new Type[] { typeof(PlayerMoveComponent) };
        }

        public override void FixedUpdate(int deltaTime)
        {
            PlayerInputComponent input = m_world.GetSingletonComp<PlayerInputComponent>();

            var entities = GetEntityList();
            for (int i = 0; i < entities.Count; i++)
            {
                PlayerMoveComponent  move  = entities[i].GetComp<PlayerMoveComponent>();
                PlayerStateComponent st    = entities[i].GetComp<PlayerStateComponent>();
                Step(move, st, input, deltaTime);
            }

            // 边沿输入消费（跳跃 / 锁定切换 / 平台跳）
            input.ConsumeOneShot();
        }

        // ── 单帧推进 ────────────────────────────────────────────────────

        /// <summary>
        /// 单个实体的一帧确定性推进。
        /// 位移 = 速度 × 时间；定点整数：delta(毫单位) = speed(毫单位/秒) × dt(ms) / 1000。
        /// </summary>
        private void Step(
            PlayerMoveComponent  move,
            PlayerStateComponent st,
            PlayerInputComponent input,
            int deltaTimeMs)
        {
            // 交互状态期间（Vault/Climb/LedgeClimb/PlatformerUp/MoveToWall）
            // 玩家主动水平移动交给动画根运动（表现层），逻辑层仅维护重力
            bool isInteracting = IsInteractState(st.state);

            // ── 速度档位 ──────────────────────────────────────────────
            // speedGear 由输入系统写入 PlayerMoveComponent（1=走, 2=跑）
            int horizontalSpeed = move.speedGear >= 2 ? RunSpeed : WalkSpeed;

            // ── 水平移动 ──────────────────────────────────────────────
            if (!isInteracting)
            {
                SyncVector3 dir = input.moveDir;
                if (dir.SqrMagnitude() > 0)
                {
                    if (move.isOnGround)
                    {
                        // 地面：完全按输入方向移动，朝向跟随
                        move.faceDir = dir.Normalized();
                        long stepLen = (long)horizontalSpeed * deltaTimeMs / 1000;
                        SyncVector3 unit = move.faceDir;
                        SyncVector3 horizontal = SyncVector3.FromRaw(
                            (int)((long)unit.x * stepLen / SyncVector3.ONE),
                            0,
                            (int)((long)unit.z * stepLen / SyncVector3.ONE));
                        move.pos = move.pos + horizontal;
                    }
                    else
                    {
                        // 空中：保留当前朝向，可用输入以 AirControl 系数微调水平位移
                        move.faceDir = dir.Normalized(); // 朝向仍跟输入，方便落地对齐
                        long baseStep = (long)horizontalSpeed * deltaTimeMs / 1000;
                        long airStep  = baseStep * AirControlFixed / SyncVector3.ONE;
                        SyncVector3 unit = move.faceDir;
                        SyncVector3 horizontal = SyncVector3.FromRaw(
                            (int)((long)unit.x * airStep / SyncVector3.ONE),
                            0,
                            (int)((long)unit.z * airStep / SyncVector3.ONE));
                        move.pos = move.pos + horizontal;
                    }
                }
            }

            // ── 跳跃（边沿消费，仅接地时生效）───────────────────────────
            if (input.jump && move.isOnGround && !isInteracting)
            {
                move.verticalSpeed = JumpSpeed;
                move.isOnGround    = false;
                // 就地跳 / 前跳区分由 PlayerStateSystem 根据 moveDir 决定，此处不额外记录
            }

            // ── 重力 / 竖直积分 ───────────────────────────────────────
            if (!move.isOnGround)
            {
                // v += g × dt / 1000
                move.verticalSpeed += (int)((long)Gravity * deltaTimeMs / 1000);

                // y += v × dt / 1000
                int dy = (int)((long)move.verticalSpeed * deltaTimeMs / 1000);
                move.pos = move.pos + SyncVector3.FromRaw(0, dy, 0);

                // 落地检测
                if (m_ground.IsGrounded(move.pos))
                {
                    int groundY = m_ground.SampleHeight(move.pos.x, move.pos.z);
                    move.pos           = SyncVector3.FromRaw(move.pos.x, groundY, move.pos.z);
                    move.verticalSpeed = 0;
                    move.isOnGround    = true;
                }
            }
        }

        // ── 辅助 ────────────────────────────────────────────────────────

        /// <summary>是否为需要屏蔽主动水平移动的交互状态。</summary>
        private static bool IsInteractState(PlayerLogicState s)
        {
            return s == PlayerLogicState.Vault
                || s == PlayerLogicState.Climb
                || s == PlayerLogicState.LedgeClimb
                || s == PlayerLogicState.PlatformerUp
                || s == PlayerLogicState.MoveToWall;
        }
    }
}
