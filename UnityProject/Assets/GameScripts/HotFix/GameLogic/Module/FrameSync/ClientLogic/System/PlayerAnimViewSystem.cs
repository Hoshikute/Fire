// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using System;
using Animancer;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家动画表现系统（表现层，渲染帧驱动）。
    /// 读取逻辑层的 PlayerStateComponent / PlayerMoveComponent（只读），驱动 Animancer 播放对应动画。
    ///
    /// 对齐参考项目 A:\animator-third-person-controller 的动画映射：
    ///   - MoveStart 按 faceDir 与 moveDir 夹角分 8 方向选 clip
    ///   - MoveEnd 按左右脚骨骼位置选 L/R clip
    ///   - PlatformerUp 三阶段（start → loop → downLoop）
    ///   - Jump/JumpInPlace 区分前跳 / 原地跳
    ///
    /// 逻辑与表现分离铁律：
    ///   - 本系统对所有逻辑组件「只读不写」。
    ///   - 动画过渡、混合树参数、idle 轮播等表现细节全在此处，不进逻辑层。
    ///   - 旋转插值（朝向）由 PlayerViewSystem 负责，本系统只管动画剪辑。
    ///
    /// 动画剪辑通过 PlayerAnimConfig（ScriptableObject）注入，不硬编码路径。
    /// PlayerViewComponent 持有 Animancer 实例引用（animancer 字段）。
    /// </summary>
    public class PlayerAnimViewSystem : ViewSystemBase
    {
        public override Type[] GetFilter()
        {
            return new Type[]
            {
                typeof(PlayerMoveComponent),
                typeof(PlayerStateComponent),
                typeof(PlayerViewComponent),
            };
        }

        // ── Update ────────────────────────────────────────────────────────

        public override void Update(int deltaTime)
        {
            PlayerInputComponent input = m_world.GetSingletonComp<PlayerInputComponent>();
            var entities = GetEntityList();
            for (int i = 0; i < entities.Count; i++)
            {
                PlayerMoveComponent  move = entities[i].GetComp<PlayerMoveComponent>();
                PlayerStateComponent st   = entities[i].GetComp<PlayerStateComponent>();
                PlayerViewComponent  view = entities[i].GetComp<PlayerViewComponent>();

                if (view.animancer == null || view.animConfig == null)
                    continue;

                // 首次执行时无条件播放当前状态动画
                if (!view.animInitialized)
                {
                    view.animInitialized = true;
                    PlayAnim(move, st, view, input);
                    continue;
                }

                // 状态切换时触发播放
                if (st.state != st.prevState)
                {
                    PlayAnim(move, st, view, input);
                }
                else
                {
                    // 同状态内持续更新：PlatformerUp 阶段切换
                    UpdateOngoingState(move, st, view, input);
                }
            }
        }

        /// <summary>同状态内的持续更新（多阶段动画切换等）。</summary>
        private void UpdateOngoingState(
            PlayerMoveComponent move,
            PlayerStateComponent st,
            PlayerViewComponent view,
            PlayerInputComponent input)
        {
            // PlatformerUp 阶段推进
            if (st.state == PlayerLogicState.PlatformerUp)
            {
                AnimancerComponent animancer = view.animancer;
                PlayerAnimConfig   cfg       = view.animConfig;

                if (view.platformerUpPhase == 0)
                {
                    // start 阶段：等待动画播放结束 → 进入 loop
                    var currentState = animancer.States.Current;
                    if (currentState != null && currentState.NormalizedTime >= 1f)
                    {
                        view.platformerUpPhase = 1;
                        PlayIfNotNull(animancer, cfg.platformerUpLoop);
                    }
                }
                else if (view.platformerUpPhase == 1)
                {
                    // loop 阶段：检测 verticalSpeed < 0 → 进入 downLoop
                    if (move.verticalSpeed < 0)
                    {
                        view.platformerUpPhase = 2;
                        PlayIfNotNull(animancer, cfg.platformerDownLoop);
                    }
                }
            }
        }

        // ── 动画分发 ────────────────────────────────────────────────────

        private void PlayAnim(
            PlayerMoveComponent move,
            PlayerStateComponent st,
            PlayerViewComponent view,
            PlayerInputComponent input)
        {
            AnimancerComponent animancer = view.animancer;
            PlayerAnimConfig   cfg       = view.animConfig;

            Log.Info($"[PlayerAnimViewSystem] PlayAnim state={st.state} prevState={st.prevState}");

            // 重置多阶段状态（进入新状态时）
            view.platformerUpPhase = 0;

            switch (st.state)
            {
                // ── 地面常态 ────────────────────────────────────────────
                case PlayerLogicState.Idle:
                    PlayIfNotNull(animancer, cfg.idle);
                    break;

                case PlayerLogicState.MoveStart:
                    PlayMoveStartDirectional(move, view, input);
                    break;

                case PlayerLogicState.MoveLoop:
                    PlayIfNotNull(animancer, cfg.moveLoop);
                    break;

                case PlayerLogicState.MoveEnd:
                    PlayMoveEndFooted(view);
                    break;

                case PlayerLogicState.MoveToWall:
                    PlayMoveToWall(view);
                    break;

                // ── 锁定模式 ────────────────────────────────────────────
                case PlayerLogicState.LockIdle:
                    PlayIfNotNull(animancer, cfg.lockIdle ?? cfg.idle);
                    break;

                // ── 空中 ────────────────────────────────────────────────
                case PlayerLogicState.Jump:
                    PlayIfNotNull(animancer, cfg.jumpForward);
                    break;

                case PlayerLogicState.JumpInPlace:
                    PlayIfNotNull(animancer, cfg.jumpInPlace);
                    break;

                case PlayerLogicState.Fall:
                    PlayFall(view);
                    break;

                case PlayerLogicState.Land:
                    PlayIfNotNull(animancer, cfg.land);
                    break;

                // ── 交互 / 攀爬 ─────────────────────────────────────────
                case PlayerLogicState.Vault:
                    PlayIfNotNull(animancer, cfg.vault);
                    break;

                case PlayerLogicState.Climb:
                    PlayIfNotNull(animancer, cfg.climb);
                    break;

                case PlayerLogicState.LedgeClimb:
                    PlayIfNotNull(animancer, cfg.ledgeClimb);
                    break;

                case PlayerLogicState.PlatformerUp:
                    view.platformerUpPhase = 0;
                    PlayIfNotNull(animancer, cfg.platformerUpStart);
                    break;

                default:
                    Log.Warning($"[PlayerAnimViewSystem] 未处理的状态：{st.state}");
                    break;
            }
        }

        // ── 子状态分发 ─────────────────────────────────────────────────

        /// <summary>
        /// MoveStart 按 8 方向选择 clip。
        /// 计算 faceDir（当前朝向）与 input.moveDir（期望移动方向）的夹角，
        /// 按参考项目的 22.5°~157.5° 区间选择对应 clip。
        /// </summary>
        private void PlayMoveStartDirectional(
            PlayerMoveComponent move,
            PlayerViewComponent view,
            PlayerInputComponent input)
        {
            PlayerAnimConfig cfg = view.animConfig;
            AnimancerComponent animancer = view.animancer;

            // 如果没有移动输入，fallback 到正前方向
            if (input.moveDir.SqrMagnitude() == 0)
            {
                PlayIfNotNull(animancer, cfg.moveStart_F);
                return;
            }

            // 计算 faceDir 与 moveDir 的夹角（-180° ~ 180°）
            // 定点 → float：除以 ONE(1000)
            float fx = move.faceDir.x / 1000f;
            float fz = move.faceDir.z / 1000f;
            float mx = input.moveDir.x / 1000f;
            float mz = input.moveDir.z / 1000f;

            float dot   = fx * mx + fz * mz;
            float cross = fx * mz - fz * mx;
            float angle = Mathf.Atan2(cross, dot) * Mathf.Rad2Deg;

            TransitionAsset clip = SelectMoveStartClip(cfg, angle);
            PlayIfNotNull(animancer, clip);
        }

        /// <summary>根据角度区间选择 MoveStart clip。</summary>
        private static TransitionAsset SelectMoveStartClip(PlayerAnimConfig cfg, float angle)
        {
            if (angle >= -22.5f && angle < 22.5f)
                return cfg.moveStart_F;
            if (angle >= 22.5f && angle < 67.5f)
                return cfg.moveStart_R45;
            if (angle >= 67.5f && angle < 112.5f)
                return cfg.moveStart_R90;
            if (angle >= 112.5f && angle < 157.5f)
                return cfg.moveStart_R135;
            if (angle >= 157.5f || angle < -157.5f)
                return cfg.moveStart_R180;
            if (angle >= -157.5f && angle < -112.5f)
                return cfg.moveStart_L135;
            if (angle >= -112.5f && angle < -67.5f)
                return cfg.moveStart_L90;
            if (angle >= -67.5f && angle < -22.5f)
                return cfg.moveStart_L45;
            // fallback
            return cfg.moveStart_F;
        }

        /// <summary>
        /// MoveEnd 按左右脚选择 clip。
        /// 检查 Animator 中左/右脚骨骼的本地 Z 位置，决定哪只脚在前。
        /// </summary>
        private void PlayMoveEndFooted(PlayerViewComponent view)
        {
            PlayerAnimConfig cfg = view.animConfig;
            AnimancerComponent animancer = view.animancer;

            Animator animator = animancer.Animator;
            if (animator == null || !animator.isHuman)
            {
                // 非 Humanoid：fallback 到 moveEnd_L
                PlayIfNotNull(animancer, cfg.moveEnd_L);
                return;
            }

            Transform leftFoot  = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (leftFoot != null && rightFoot != null)
            {
                Vector3 leftLocal  = animator.transform.InverseTransformPoint(leftFoot.position);
                Vector3 rightLocal = animator.transform.InverseTransformPoint(rightFoot.position);

                if (leftLocal.z > rightLocal.z)
                {
                    PlayIfNotNull(animancer, cfg.moveEnd_L);
                }
                else
                {
                    PlayIfNotNull(animancer, cfg.moveEnd_R);
                }
            }
            else
            {
                // 无法获取骨骼：fallback
                PlayIfNotNull(animancer, cfg.moveEnd_L);
            }
        }

        /// <summary>
        /// Fall 状态：播放 fallStart → OnEnd → fallLoop。
        /// 通过 AnimancerEvent 事件回调实现链接。
        /// </summary>
        private void PlayFall(PlayerViewComponent view)
        {
            PlayerAnimConfig cfg = view.animConfig;
            AnimancerComponent animancer = view.animancer;

            if (cfg.fallStart != null)
            {
                var state = animancer.Play(cfg.fallStart);
                state.Events(view.viewRoot).OnEnd = () =>
                {
                    if (cfg.fallLoop != null)
                        animancer.Play(cfg.fallLoop);
                };
            }
            else
            {
                PlayIfNotNull(animancer, cfg.fallLoop);
            }
        }

        /// <summary>
        /// MoveToWall：优先 moveToWall，为空时回退到 moveEnd_L。
        /// 对齐参考项目（PlayerMoveToWallState 播放 MoveEndData.moveToWall）。
        /// </summary>
        private void PlayMoveToWall(PlayerViewComponent view)
        {
            PlayerAnimConfig cfg = view.animConfig;
            AnimancerComponent animancer = view.animancer;

            PlayIfNotNull(animancer, cfg.moveToWall ?? cfg.moveEnd_L);
        }

        // ── 工具 ────────────────────────────────────────────────────────

        private static void PlayIfNotNull(AnimancerComponent animancer, TransitionAsset asset)
        {
            if (asset != null)
                animancer.Play(asset);
        }
    }
}
