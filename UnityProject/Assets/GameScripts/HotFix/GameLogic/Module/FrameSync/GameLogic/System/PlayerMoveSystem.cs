
using System;
using TEngine;

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

        /// <summary>
        /// 最大可通行坡度角的余弦值（定点，单位 = SyncVector3.ONE = 1000）。
        /// cos(45°) ≈ 0.707 → 707。当地面法线 y 分量低于此值时视为不可通行（太陡）。
        /// </summary>
        private const int MaxSlopeCos = 707;

        // ── 移动手感参数（定点）─────────────────────────────────────────
        /// <summary>加速度（毫单位/秒²）。每逻辑帧回加速这么多直到达到标称速度。</summary>
        private const int Acceleration = 16000;

        /// <summary>减速度/惯性衰减（毫单位/秒²）。松开输入后每帧减速。</summary>
        private const int Deceleration = 20000;

        /// <summary>每逻辑帧最大转向角（定点，毫弧度 ≈ cos/sin 直接用向量）。</summary>
        private const int MaxTurnRate = 400; // 约 0.4 弧度/帧（~23°/帧，200ms 帧对应 ~115°/秒）

        // ── 诊断：前 N 帧输出状态 ────────────────────────────────────────
        private int m_diagFrameCount = 0;
        private const int DiagMaxFrames = 10;

        // ── 确定性地面服务 ──────────────────────────────────────────────
        private IDeterministicGround m_ground;

        /// <summary>确定性碰撞世界（逻辑层服务）。</summary>
        private ICollisionWorld m_collision;

        /// <summary>攀爬/翻越轨迹配置（由 PlayerWorld/Entry 注入）。</summary>
        public ClimbConfig ClimbConfig { get; set; }

        public override void Init()
        {
            base.Init();
            m_ground = new FlatGround(0);
            m_collision = new SimpleCollisionWorld();
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

                // 诊断：前 N 帧输出关键状态
                if (m_diagFrameCount < DiagMaxFrames)
                {
                    Log.Info($"[PlayerMoveSystem] F#{m_world.FrameCount} entity#{entities[i].ID} " +
                             $"pos=({move.pos.x},{move.pos.y},{move.pos.z}) " +
                             $"isOnGround={move.isOnGround} vSpeed={move.verticalSpeed} " +
                             $"curSpeed={move.currentSpeed} speedGear={input.speedGear} " +
                             $"state={st.state} framesInState={st.framesInState}");
                    m_diagFrameCount++;
                }

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

            // ── 攀爬/翻越轨迹推进 ──────────────────────────────────────────
            if (isInteracting && ClimbConfig != null)
            {
                var trajectory = ClimbConfig.GetTrajectory(st.state);
                if (trajectory != null && st.framesInState < trajectory.Count)
                {
                    // 逐帧按轨迹表推进位置
                    SyncVector3 delta = trajectory[st.framesInState].ToDelta();
                    move.pos = move.pos + delta;

                    // 轨迹结束后自动切回 Idle（由 PlayerStateSystem 处理）
                    // 此处只推进位移，不改变状态
                }
            }

            // ── 速度档位 ──────────────────────────────────────────────
            // speedGear 由输入采集系统写入 PlayerInputComponent（1=走, 2=跑）
            int targetSpeed = input.speedGear >= 2 ? RunSpeed : WalkSpeed;

            // ── 加减速曲线 ────────────────────────────────────────────
            bool hasMoveInput = input.moveDir.SqrMagnitude() > 0;
            if (hasMoveInput)
            {
                // 加速：向目标速度逼近
                int accelDelta = (int)((long)Acceleration * deltaTimeMs / 1000);
                if (move.currentSpeed < targetSpeed)
                {
                    move.currentSpeed += accelDelta;
                    if (move.currentSpeed > targetSpeed)
                        move.currentSpeed = targetSpeed;
                }
                else if (move.currentSpeed > targetSpeed)
                {
                    move.currentSpeed = targetSpeed; // 换档时直接降速
                }
            }
            else
            {
                // 惯性衰减：松开输入后逐步减速
                if (move.currentSpeed > 0)
                {
                    int decelDelta = (int)((long)Deceleration * deltaTimeMs / 1000);
                    move.currentSpeed -= decelDelta;
                    if (move.currentSpeed < 0)
                        move.currentSpeed = 0;
                }
            }

            int horizontalSpeed = move.currentSpeed;

            // ── 水平移动 ──────────────────────────────────────────────
            if (!isInteracting)
            {
                SyncVector3 dir = input.moveDir;
                if (dir.SqrMagnitude() > 0)
                {
                    if (move.isOnGround)
                    {
                        // 地面：查询法线 + 坡度检测 + 斜面投影位移
                        SyncVector3 groundNormal = m_ground.GetNormal(move.pos.x, move.pos.z);

                        // 坡度检测：若法线 y 分量 < MaxSlopeCos（太陡），阻止水平移动
                        if (groundNormal.y < MaxSlopeCos)
                        {
                            // 坡度过陡，不执行水平移动，朝向仍平滑转向输入方向
                            move.faceDir = RotateTowards(move.faceDir, dir.Normalized());
                        }
                        else
                        {
                            move.faceDir = RotateTowards(move.faceDir, dir.Normalized());
                            long stepLen = (long)horizontalSpeed * deltaTimeMs / 1000;
                            SyncVector3 unit = move.faceDir;
                            SyncVector3 horizontal = SyncVector3.FromRaw(
                                (int)((long)unit.x * stepLen / SyncVector3.ONE),
                                0,
                                (int)((long)unit.z * stepLen / SyncVector3.ONE));

                            // 斜面投影：将水平位移投影到斜面切平面
                            // projection = v - ((v·n) / (n·n)) * n
                            // n·n = ONE² = 1000000 (法线是单位向量)
                            long dot = (long)horizontal.x * groundNormal.x
                                     + (long)horizontal.y * groundNormal.y
                                     + (long)horizontal.z * groundNormal.z;
                            long scaleX = dot * groundNormal.x / 1000000;
                            long scaleY = dot * groundNormal.y / 1000000;
                            long scaleZ = dot * groundNormal.z / 1000000;
                            SyncVector3 slopeDelta = SyncVector3.FromRaw(
                                (int)(horizontal.x - scaleX),
                                (int)(horizontal.y - scaleY),
                                (int)(horizontal.z - scaleZ));
                            move.pos = move.pos + slopeDelta;
                        }
                    }
                    else
                    {
                        // 空中：保留当前朝向，可用输入以 AirControl 系数微调水平位移
                        move.faceDir = RotateTowards(move.faceDir, dir.Normalized());
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

            // ── 碰撞检测与响应 ────────────────────────────────────────
            // 在水平位移后执行胶囊扫掠，分离穿透 + 沿表面滑动
            if (m_collision != null)
            {
                SyncVector3 preMovePos = move.pos;
                CollisionResult col = m_collision.CapsuleSweep(
                    preMovePos, move.pos,
                    move.capsuleRadius, move.capsuleHeight);

                if (col.hit)
                {
                    // 分离：沿碰撞法线推出穿透深度
                    long pushX = (long)col.normal.x * col.penetration / SyncVector3.ONE;
                    long pushY = (long)col.normal.y * col.penetration / SyncVector3.ONE;
                    long pushZ = (long)col.normal.z * col.penetration / SyncVector3.ONE;
                    move.pos = SyncVector3.FromRaw(
                        (int)(move.pos.x + pushX),
                        (int)(move.pos.y + pushY),
                        (int)(move.pos.z + pushZ));

                    // 滑动：将剩余速度投影到碰撞面切平面
                    // 剩余速度 = move.pos(修正后) - preMovePos
                    // 滑动速度 = v - (v·n) * n (n 已归一化)
                    long vx = move.pos.x - preMovePos.x;
                    long vy = move.pos.y - preMovePos.y;
                    long vz = move.pos.z - preMovePos.z;
                    long vDotN = vx * col.normal.x + vy * col.normal.y + vz * col.normal.z;
                    long slideScale = vDotN / SyncVector3.ONE;
                    SyncVector3 slideDelta = SyncVector3.FromRaw(
                        (int)(vx - slideScale * col.normal.x / SyncVector3.ONE),
                        (int)(vy - slideScale * col.normal.y / SyncVector3.ONE),
                        (int)(vz - slideScale * col.normal.z / SyncVector3.ONE));
                    move.pos = move.pos + slideDelta;
                }
            }

            // ── 跳跃（边沿消费，仅接地时生效）───────────────────────────
            if (input.jump && move.isOnGround && !isInteracting)
            {
                move.verticalSpeed = JumpSpeed;
                move.isOnGround    = false;
                // 记录是否原地跳跃（无移动输入），供 PlayerStateSystem 选择 Jump vs JumpInPlace
                st.isInPlaceJump = input.moveDir.SqrMagnitude() == 0;
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

        /// <summary>
        /// 定点平滑转向：将 current 逐步转向 target，每帧最大旋转量由 MaxTurnRate 限制。
        /// 使用交叉积（y）判断旋转方向，点积判断角度差。
        /// </summary>
        private static SyncVector3 RotateTowards(SyncVector3 current, SyncVector3 target)
        {
            if (target.SqrMagnitude() == 0)
                return current;

            // 点积 = cos(angle) * |a| * |b|，两者都已归一化（|a|=|b|=ONE）
            long dot = (long)current.x * target.x + (long)current.z * target.z;
            // cos(angle) = dot / (ONE * ONE) = dot / 1000000
            long cosAngle = dot / SyncVector3.ONE;

            // 如果已非常接近（cos > MaxTurnRate 对应值），直接返回目标
            // MaxTurnRateCos = cos(MaxTurnRate) 的近似：MaxTurnRate=400 → ≈ cos(0.4) ≈ 0.92
            const int MaxTurnRateCos = 920;
            if (cosAngle >= MaxTurnRateCos * SyncVector3.ONE / 1000)
                return target;

            // 交叉积 y 分量 = a.x * b.z - a.z * b.x（判断旋转方向）
            long cross = (long)current.x * target.z - (long)current.z * target.x;
            int sign = cross >= 0 ? 1 : -1;

            // 用旋转矩阵绕 Y 轴旋转 MaxTurnRate 角
            // cosT = cos(MaxTurnRate), sinT = sin(MaxTurnRate)
            // cos(0.4) ≈ 0.921061 → 921, sin(0.4) ≈ 0.389418 → 389
            int cosT = 921;
            int sinT = 389 * sign;

            int newX = (int)(((long)current.x * cosT - (long)current.z * sinT) / SyncVector3.ONE);
            int newZ = (int)(((long)current.x * sinT + (long)current.z * cosT) / SyncVector3.ONE);

            return SyncVector3.FromRaw(newX, 0, newZ);
        }
    }
}
