
using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家状态系统（确定性逻辑层）。
    /// 在固定逻辑帧里根据 PlayerMoveComponent 的物理事实（接地 / 竖直速度 / 移动意图）
    /// 以及 PlayerStateComponent 的辅助字段（锁定 / 平台跳 / 墙体类型）
    /// 推导出 PlayerStateComponent.state，并维护「在状态内经过的逻辑帧数」。
    ///
    /// 完整对齐原 ThirdPersonController 的状态树：
    ///   Idle / MoveStart / MoveLoop / MoveEnd
    ///   Jump / JumpInPlace / Fall / Land
    ///   LockIdle
    ///   MoveToWall / Vault / Climb / LedgeClimb / PlatformerUp
    ///
    /// 设计要点：
    ///   - 状态是物理事实的「投影」，不反向驱动位移（位移由 PlayerMoveSystem 负责）。
    ///   - 所有「延时」用 framesInState 帧计数表达，禁止 Time / GameModule.Timer。
    ///   - 注册顺序排在 PlayerMoveSystem 之后（先算物理，再投影状态）。
    ///
    /// 确定性铁律：禁止 float / Time / Random；只读整数状态。
    /// </summary>
    public class PlayerStateSystem : SystemBase
    {
        // ── 帧计数阈值（200ms / 帧）────────────────────────────────────
        /// <summary>落地缓冲帧数（≈200ms）。</summary>
        private const int LandBufferFrames = 1;

        /// <summary>MoveStart 持续帧数（≈200ms）。</summary>
        private const int MoveStartFrames = 1;

        /// <summary>MoveEnd 持续帧数（≈200ms）。</summary>
        private const int MoveEndFrames = 1;

        /// <summary>
        /// Vault / Climb / LedgeClimb / PlatformerUp 最小持续帧数（≈600ms）。
        /// 防止攀爬动画还没结束就被打断切回 Idle。
        /// </summary>
        private const int InteractMinFrames = 3;

        public override Type[] GetFilter()
        {
            return new Type[] { typeof(PlayerMoveComponent), typeof(PlayerStateComponent), typeof(PlayerCommandRecordComponent) };
        }

        public override void FixedUpdate(int deltaTime)
        {
            var entities = GetEntityList();
            for (int i = 0; i < entities.Count; i++)
            {
                PlayerMoveComponent  move  = entities[i].GetComp<PlayerMoveComponent>();
                PlayerStateComponent st    = entities[i].GetComp<PlayerStateComponent>();
                PlayerCommandRecordComponent record = entities[i].GetComp<PlayerCommandRecordComponent>();
                record.EnsureDefaultCommand(entities[i].ID);
                CommandComponent command = record.GetOrForecastInput(m_world.FrameCount) as CommandComponent;

                if (command != null)
                {
                    Step(move, st, command);
                }
            }
        }

        // ── 核心推导 ────────────────────────────────────────────────────

        private void Step(PlayerMoveComponent move, PlayerStateComponent st, CommandComponent command)
        {
            // 锁定切换（边沿输入在本系统读完后统一消费）
            if (command.toggleLock)
            {
                st.isLocked = !st.isLocked;
            }

            // 平台跳请求写入状态组件（由交互触发，此处仅中继）
            if (command.platformJump)
            {
                st.platformJumpRequested = true;
            }

            PlayerLogicState next = Decide(move, st);

            if (next != st.state && !m_world.m_isRecalc)
            {
                Log.Info($"[PlayerStateSystem] F#{m_world.FrameCount} st={st.state}→{next} " +
                         $"isOnGround={move.isOnGround} vSpeed={move.verticalSpeed} " +
                         $"moveDirMag={move.moveIntentDir.SqrMagnitude()} isLocked={st.isLocked}");
            }

            st.prevState = st.state;
            if (next != st.state)
            {
                st.state        = next;
                st.framesInState = 0;
            }
            else
            {
                st.framesInState++;
            }

            // 平台跳请求消费：只要成功进入 PlatformerUp 就清掉
            if (st.state == PlayerLogicState.PlatformerUp)
            {
                st.platformJumpRequested = false;
            }
        }

        /// <summary>
        /// 状态决策。
        /// 优先级（高→低）：
        ///   交互锁定（Vault/Climb/LedgeClimb/PlatformerUp）
        ///   → 空中（Jump/JumpInPlace/Fall）
        ///   → 落地缓冲（Land）
        ///   → 平台跳请求
        ///   → 锁定模式（LockIdle）
        ///   → 靠墙过渡（MoveToWall）
        ///   → 地面常态（MoveStart/MoveLoop/MoveEnd/Idle）
        /// </summary>
        private PlayerLogicState Decide(
            PlayerMoveComponent  move,
            PlayerStateComponent st)
        {
            // ── 1. 交互动作：进入后须等最小帧数才能退出 ──────────────────
            if (IsInteractState(st.state))
            {
                if (st.framesInState < InteractMinFrames)
                    return st.state; // 锁定在交互状态内
                // 超过最小帧后，按物理现实退出
            }

            // ── 2. 空中 ───────────────────────────────────────────────────
            if (!move.isOnGround)
            {
                if (move.verticalSpeed > 0)
                {
                    // 区分就地跳和前跳：由 PlayerMoveSystem 在起跳时写入 st.isInPlaceJump
                    if (st.isInPlaceJump)
                        return PlayerLogicState.JumpInPlace;
                    return PlayerLogicState.Jump;
                }
                return PlayerLogicState.Fall;
            }

            // ── 3. 刚落地缓冲 ─────────────────────────────────────────────
            bool justLanded = st.state == PlayerLogicState.Jump
                           || st.state == PlayerLogicState.JumpInPlace
                           || st.state == PlayerLogicState.Fall;
            if (justLanded)
                return PlayerLogicState.Land;

            if (st.state == PlayerLogicState.Land && st.framesInState < LandBufferFrames)
                return PlayerLogicState.Land;

            // ── 4. 平台跳请求 ─────────────────────────────────────────────
            if (st.platformJumpRequested)
                return PlayerLogicState.PlatformerUp;

            // ── 5. 墙体交互决策 ───────────────────────────────────────────
            // wallObstructType 由碰撞检测系统（PlayerWallCheckSystem，待扩展）写入
            if (st.wallObstructType > 0 && move.isOnGround)
            {
                switch (st.wallObstructType)
                {
                    case 1: return PlayerLogicState.Vault;
                    case 2: return PlayerLogicState.Climb;
                    case 3: return PlayerLogicState.LedgeClimb;
                    default: return PlayerLogicState.MoveToWall;
                }
            }

            bool hasMoveInput = move.moveIntentDir.SqrMagnitude() > 0;

            // ── 6. 锁定模式 ───────────────────────────────────────────────
            if (st.isLocked)
            {
                // 锁定下有移动输入 → 仍走普通移动状态（动画层面由表现层区分）
                // 无输入 → LockIdle
                if (!hasMoveInput)
                    return PlayerLogicState.LockIdle;
            }

            // ── 7. 地面常态（MoveStart / MoveLoop / MoveEnd / Idle）────────
            if (hasMoveInput)
            {
                // 从静止/Land/LockIdle 开始移动 → MoveStart
                bool wasStill = st.state == PlayerLogicState.Idle
                             || st.state == PlayerLogicState.Land
                             || st.state == PlayerLogicState.LockIdle
                             || st.state == PlayerLogicState.MoveEnd;
                if (wasStill)
                    return PlayerLogicState.MoveStart;

                // MoveStart 持续了足够帧 → 进入 MoveLoop
                if (st.state == PlayerLogicState.MoveStart && st.framesInState >= MoveStartFrames)
                    return PlayerLogicState.MoveLoop;

                // 已在移动中 → 保持 MoveLoop
                if (st.state == PlayerLogicState.MoveLoop || st.state == PlayerLogicState.MoveStart)
                    return st.state;

                return PlayerLogicState.MoveLoop;
            }
            else
            {
                // 从 MoveLoop → MoveEnd
                if (st.state == PlayerLogicState.MoveLoop || st.state == PlayerLogicState.MoveStart)
                    return PlayerLogicState.MoveEnd;

                // MoveEnd 持续了足够帧 → Idle
                if (st.state == PlayerLogicState.MoveEnd && st.framesInState >= MoveEndFrames)
                    return PlayerLogicState.Idle;

                if (st.state == PlayerLogicState.MoveEnd)
                    return PlayerLogicState.MoveEnd;

                return PlayerLogicState.Idle;
            }
        }

        /// <summary>是否为需要最小帧锁定的交互状态。</summary>
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
