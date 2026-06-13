
using System;
using System.Collections.Generic;
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
        private const string StandValueParameterName = "StandValue";
        private const string SpeedValueParameterName = "SpeedValue";
        private const string RotationValueParameterName = "RotationValue";
        private const string LockValueParameterName = "LockValue";

        private const float CrouchingStanceParameter = 0f;
        private const float StandingStanceParameter = 1f;
        private const float WalkSpeedParameter = 1f;
        private const float RunSpeedParameter = 2f;
        private const float UnlockedParameter = 0f;
        private const float LockedParameter = 1f;

        private const int CrouchingStanceChildIndex = 0;
        private const int StandingStanceChildIndex = 1;
        private const int DefaultIdleManualChildIndex = 0;
        private const float ForwardRunRotationValue = 0f;

        private readonly HashSet<string> _warningKeys = new HashSet<string>();

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
                EntityBase entity = entities[i];
                PlayerMoveComponent  move = entity.GetComp<PlayerMoveComponent>();
                PlayerStateComponent st   = entity.GetComp<PlayerStateComponent>();
                PlayerViewComponent  view = entity.GetComp<PlayerViewComponent>();

                if (!EnsureAnimationDependencies(entity.ID, view))
                    continue;

                RefreshLocomotionParameters(entity.ID, move, st, view, input);

                // 首次执行时无条件播放当前状态动画
                if (!view.animInitialized)
                {
                    view.animInitialized = true;
                    PlayAnim(entity.ID, move, st, view, input);
                    continue;
                }

                // 状态切换时触发播放
                if (st.state != st.prevState)
                {
                    PlayAnim(entity.ID, move, st, view, input);
                }
                else
                {
                    // 同状态内持续更新：PlatformerUp 阶段切换
                    UpdateOngoingState(entity.ID, move, st, view, input);
                }
            }
        }

        /// <summary>同状态内的持续更新（多阶段动画切换等）。</summary>
        private void UpdateOngoingState(
            int entityId,
            PlayerMoveComponent move,
            PlayerStateComponent st,
            PlayerViewComponent view,
            PlayerInputComponent input)
        {
            if (st.state == PlayerLogicState.Idle || st.state == PlayerLogicState.LockIdle)
            {
                ApplyIdleStanceMixerWeights(view.animancer.States.Current, entityId, st.isLocked, input.isCrouching);
            }

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
                        PlayOrWarn(animancer, cfg.platformerUpLoop, entityId, st.state, nameof(cfg.platformerUpLoop));
                    }
                }
                else if (view.platformerUpPhase == 1)
                {
                    // loop 阶段：检测 verticalSpeed < 0 → 进入 downLoop
                    if (move.verticalSpeed < 0)
                    {
                        view.platformerUpPhase = 2;
                        PlayOrWarn(animancer, cfg.platformerDownLoop, entityId, st.state, nameof(cfg.platformerDownLoop));
                    }
                }
            }
        }

        // ── 动画分发 ────────────────────────────────────────────────────

        private void PlayAnim(
            int entityId,
            PlayerMoveComponent move,
            PlayerStateComponent st,
            PlayerViewComponent view,
            PlayerInputComponent input)
        {
            AnimancerComponent animancer = view.animancer;
            PlayerAnimConfig   cfg       = view.animConfig;

            // 重置多阶段状态（进入新状态时）
            view.platformerUpPhase = 0;

            switch (st.state)
            {
                // ── 地面常态 ────────────────────────────────────────────
                case PlayerLogicState.Idle:
                    PlayIdleStance(animancer, cfg.idle, entityId, st.state, nameof(cfg.idle), st.isLocked, input.isCrouching);
                    break;

                case PlayerLogicState.MoveStart:
                    PlayMoveStartDirectional(entityId, move, st, view, input);
                    break;

                case PlayerLogicState.MoveLoop:
                    PlayOrWarn(animancer, cfg.moveLoop, entityId, st.state, nameof(cfg.moveLoop));
                    break;

                case PlayerLogicState.MoveEnd:
                    PlayMoveEndFooted(entityId, st, view);
                    break;

                case PlayerLogicState.MoveToWall:
                    PlayMoveToWall(entityId, st, view);
                    break;

                // ── 锁定模式 ────────────────────────────────────────────
                case PlayerLogicState.LockIdle:
                    PlayIdleStance(animancer, cfg.GetLockIdleTransition(), entityId, st.state, $"{nameof(cfg.lockIdle)} or {nameof(cfg.idle)} fallback", st.isLocked, input.isCrouching);
                    break;

                // ── 空中 ────────────────────────────────────────────────
                case PlayerLogicState.Jump:
                    PlayOrWarn(animancer, cfg.jumpForward, entityId, st.state, nameof(cfg.jumpForward));
                    break;

                case PlayerLogicState.JumpInPlace:
                    PlayOrWarn(animancer, cfg.jumpInPlace, entityId, st.state, nameof(cfg.jumpInPlace));
                    break;

                case PlayerLogicState.Fall:
                    PlayFall(entityId, st, view);
                    break;

                case PlayerLogicState.Land:
                    PlayOrWarn(animancer, cfg.land, entityId, st.state, nameof(cfg.land));
                    break;

                // ── 交互 / 攀爬 ─────────────────────────────────────────
                case PlayerLogicState.Vault:
                    PlayOrWarn(animancer, cfg.vault, entityId, st.state, nameof(cfg.vault));
                    break;

                case PlayerLogicState.Climb:
                    PlayOrWarn(animancer, cfg.climb, entityId, st.state, nameof(cfg.climb));
                    break;

                case PlayerLogicState.LedgeClimb:
                    PlayOrWarn(animancer, cfg.ledgeClimb, entityId, st.state, nameof(cfg.ledgeClimb));
                    break;

                case PlayerLogicState.PlatformerUp:
                    view.platformerUpPhase = 0;
                    PlayOrWarn(animancer, cfg.platformerUpStart, entityId, st.state, nameof(cfg.platformerUpStart));
                    break;

                default:
                    Log.Warning($"[PlayerAnimViewSystem] 未处理的状态：{st.state}");
                    break;
            }

            RefreshLocomotionParameters(entityId, move, st, view, input);
        }

        // ── 子状态分发 ─────────────────────────────────────────────────

        /// <summary>
        /// MoveStart 按 8 方向选择 clip。
        /// 计算 faceDir（当前朝向）与 input.moveDir（期望移动方向）的夹角，
        /// 按参考项目的 22.5°~157.5° 区间选择对应 clip。
        /// </summary>
        private void PlayMoveStartDirectional(
            int entityId,
            PlayerMoveComponent move,
            PlayerStateComponent st,
            PlayerViewComponent view,
            PlayerInputComponent input)
        {
            PlayerAnimConfig cfg = view.animConfig;
            AnimancerComponent animancer = view.animancer;

            // 如果没有移动输入，fallback 到正前方向
            if (input.moveDir.SqrMagnitude() == 0)
            {
                PlayOrWarn(animancer, cfg.moveStart_F, entityId, st.state, nameof(cfg.moveStart_F));
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

            string fieldName;
            TransitionAsset clip = SelectMoveStartClip(cfg, angle, out fieldName);
            PlayOrWarn(animancer, clip, entityId, st.state, fieldName);
        }

        /// <summary>根据角度区间选择 MoveStart clip。</summary>
        private static TransitionAsset SelectMoveStartClip(PlayerAnimConfig cfg, float angle, out string fieldName)
        {
            if (angle >= -22.5f && angle < 22.5f)
            {
                fieldName = nameof(cfg.moveStart_F);
                return cfg.moveStart_F;
            }
            if (angle >= 22.5f && angle < 67.5f)
            {
                fieldName = nameof(cfg.moveStart_R45);
                return cfg.moveStart_R45;
            }
            if (angle >= 67.5f && angle < 112.5f)
            {
                fieldName = nameof(cfg.moveStart_R90);
                return cfg.moveStart_R90;
            }
            if (angle >= 112.5f && angle < 157.5f)
            {
                fieldName = nameof(cfg.moveStart_R135);
                return cfg.moveStart_R135;
            }
            if (angle >= 157.5f || angle < -157.5f)
            {
                fieldName = nameof(cfg.moveStart_R180);
                return cfg.moveStart_R180;
            }
            if (angle >= -157.5f && angle < -112.5f)
            {
                fieldName = nameof(cfg.moveStart_L135);
                return cfg.moveStart_L135;
            }
            if (angle >= -112.5f && angle < -67.5f)
            {
                fieldName = nameof(cfg.moveStart_L90);
                return cfg.moveStart_L90;
            }
            if (angle >= -67.5f && angle < -22.5f)
            {
                fieldName = nameof(cfg.moveStart_L45);
                return cfg.moveStart_L45;
            }
            // fallback
            fieldName = nameof(cfg.moveStart_F);
            return cfg.moveStart_F;
        }

        /// <summary>
        /// MoveEnd 按左右脚选择 clip。
        /// 检查 Animator 中左/右脚骨骼的本地 Z 位置，决定哪只脚在前。
        /// </summary>
        private void PlayMoveEndFooted(int entityId, PlayerStateComponent st, PlayerViewComponent view)
        {
            PlayerAnimConfig cfg = view.animConfig;
            AnimancerComponent animancer = view.animancer;

            Animator animator = animancer.Animator;
            if (animator == null || !animator.isHuman)
            {
                // 非 Humanoid：fallback 到 moveEnd_L
                PlayOrWarn(animancer, cfg.moveEnd_L, entityId, st.state, nameof(cfg.moveEnd_L));
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
                    PlayOrWarn(animancer, cfg.moveEnd_L, entityId, st.state, nameof(cfg.moveEnd_L));
                }
                else
                {
                    PlayOrWarn(animancer, cfg.moveEnd_R, entityId, st.state, nameof(cfg.moveEnd_R));
                }
            }
            else
            {
                // 无法获取骨骼：fallback
                PlayOrWarn(animancer, cfg.moveEnd_L, entityId, st.state, nameof(cfg.moveEnd_L));
            }
        }

        /// <summary>
        /// Fall 状态：播放 fallStart → OnEnd → fallLoop。
        /// 通过 AnimancerEvent 事件回调实现链接。
        /// </summary>
        private void PlayFall(int entityId, PlayerStateComponent st, PlayerViewComponent view)
        {
            PlayerAnimConfig cfg = view.animConfig;
            AnimancerComponent animancer = view.animancer;

            if (cfg.fallStart != null)
            {
                var state = animancer.Play(cfg.fallStart);
                state.Events(view.viewRoot).OnEnd = () =>
                {
                    if (cfg.fallLoop != null)
                    {
                        animancer.Play(cfg.fallLoop);
                    }
                    else
                    {
                        WarnMissingTransition(entityId, st.state, nameof(cfg.fallLoop));
                    }
                };
            }
            else
            {
                PlayOrWarn(animancer, cfg.fallLoop, entityId, st.state, nameof(cfg.fallLoop));
            }
        }

        /// <summary>
        /// MoveToWall：优先 moveToWall，为空时回退到 moveEnd_L。
        /// 对齐参考项目（PlayerMoveToWallState 播放 MoveEndData.moveToWall）。
        /// </summary>
        private void PlayMoveToWall(int entityId, PlayerStateComponent st, PlayerViewComponent view)
        {
            PlayerAnimConfig cfg = view.animConfig;
            AnimancerComponent animancer = view.animancer;

            PlayOrWarn(animancer, cfg.GetMoveToWallTransition(), entityId, st.state, $"{nameof(cfg.moveToWall)} or {nameof(cfg.moveEnd_L)} fallback");
        }

        // ── 工具 ────────────────────────────────────────────────────────

        private void RefreshLocomotionParameters(
            int entityId,
            PlayerMoveComponent move,
            PlayerStateComponent st,
            PlayerViewComponent view,
            PlayerInputComponent input)
        {
            AnimancerComponent animancer = view.animancer;

            float standValue = input.isCrouching ? CrouchingStanceParameter : StandingStanceParameter;
            float speedValue = input.speedGear >= 2 ? RunSpeedParameter : WalkSpeedParameter;
            float rawRotationValue = CalculateRotationValue(move, input);
            float rotationValue = CalculateEffectiveRotationValue(st, input, speedValue, rawRotationValue);
            float lockValue = st.isLocked ? LockedParameter : UnlockedParameter;

            animancer.Parameters.SetValue(StandValueParameterName, standValue);
            animancer.Parameters.SetValue(SpeedValueParameterName, speedValue);
            animancer.Parameters.SetValue(RotationValueParameterName, rotationValue);
            animancer.Parameters.SetValue(LockValueParameterName, lockValue);
        }

        private static float CalculateEffectiveRotationValue(PlayerStateComponent st, PlayerInputComponent input, float speedValue, float rawRotationValue)
        {
            if (ShouldUseForwardRunRotation(st, input, speedValue))
            {
                return ForwardRunRotationValue;
            }

            return rawRotationValue;
        }

        private static bool ShouldUseForwardRunRotation(PlayerStateComponent st, PlayerInputComponent input, float speedValue)
        {
            return st.state == PlayerLogicState.MoveLoop
                && !st.isLocked
                && speedValue >= RunSpeedParameter
                && input.moveDir.SqrMagnitude() != 0;
        }

        private static float CalculateRotationValue(PlayerMoveComponent move, PlayerInputComponent input)
        {
            if (input.moveDir.SqrMagnitude() == 0 || move.faceDir.SqrMagnitude() == 0)
            {
                return 0f;
            }

            float fx = move.faceDir.x / 1000f;
            float fz = move.faceDir.z / 1000f;
            float mx = input.moveDir.x / 1000f;
            float mz = input.moveDir.z / 1000f;

            float dot = fx * mx + fz * mz;
            float cross = fx * mz - fz * mx;
            return Mathf.Atan2(cross, dot);
        }

        private void PlayIdleStance(
            AnimancerComponent animancer,
            TransitionAsset asset,
            int entityId,
            PlayerLogicState state,
            string fieldName,
            bool isLocked,
            bool isCrouching)
        {
            if (asset == null)
            {
                WarnMissingTransition(entityId, state, fieldName);
                return;
            }

            AnimancerState idleState = animancer.Play(asset);
            ApplyIdleStanceMixerWeights(idleState, entityId, isLocked, isCrouching);
        }

        private void ApplyIdleStanceMixerWeights(AnimancerState idleState, int entityId, bool isLocked, bool isCrouching)
        {
            if (!TryApplyIdleStanceMixerWeights(idleState, entityId, isLocked, isCrouching, 0))
            {
                WarnOnce($"idle-stance-mixer:{entityId}", $"[CODEX_LOG] Player idle stance mixer not found. entity={entityId}, stateType={idleState?.GetType().Name ?? "null"}.");
            }
        }

        private bool TryApplyIdleStanceMixerWeights(AnimancerState state, int entityId, bool isLocked, bool isCrouching, int depth)
        {
            if (state is not LinearMixerState mixer)
            {
                return false;
            }

            string parameterName = mixer.ParameterName != null ? mixer.ParameterName.ToString() : string.Empty;
            if (parameterName == LockValueParameterName)
            {
                float lockValue = isLocked ? LockedParameter : UnlockedParameter;
                int lockChildIndex = isLocked ? 1 : 0;
                mixer.Parameter = lockValue;
                mixer.RecalculateWeights();

                if (mixer.ChildCount <= lockChildIndex)
                {
                    WarnOnce($"idle-lock-child:{entityId}", $"[CODEX_LOG] Player idle lock child missing. entity={entityId}, childCount={mixer.ChildCount}, requiredIndex={lockChildIndex}.");
                    return false;
                }

                return TryApplyIdleStanceMixerWeights(mixer.GetChild(lockChildIndex), entityId, isLocked, isCrouching, depth + 1);
            }

            if (parameterName != StandValueParameterName && depth > 0)
            {
                return false;
            }

            float standValue = isCrouching ? CrouchingStanceParameter : StandingStanceParameter;
            int stanceChildIndex = isCrouching ? CrouchingStanceChildIndex : StandingStanceChildIndex;
            mixer.Parameter = standValue;
            mixer.RecalculateWeights();

            if (mixer.ChildCount <= stanceChildIndex)
            {
                WarnOnce($"idle-stance-child:{entityId}", $"[CODEX_LOG] Player idle stance child missing. entity={entityId}, childCount={mixer.ChildCount}, requiredIndex={stanceChildIndex}.");
                return false;
            }

            if (mixer.GetChild(stanceChildIndex) is ManualMixerState innerMixer)
            {
                SelectManualMixerChild(innerMixer, DefaultIdleManualChildIndex);
            }

            return true;
        }

        private static void SelectManualMixerChild(ManualMixerState mixer, int selectedIndex)
        {
            for (int i = 0; i < mixer.ChildCount; i++)
            {
                AnimancerState child = mixer.GetChild(i);
                if (i == selectedIndex)
                {
                    child.SetWeight(1f);
                    child.Play();
                }
                else
                {
                    child.SetWeight(0f);
                    child.Stop();
                }
            }
        }

        private bool EnsureAnimationDependencies(int entityId, PlayerViewComponent view)
        {
            if (view.animancer == null)
            {
                WarnOnce($"animancer-null:{entityId}", $"[PlayerAnimViewSystem] animancer == null: entity={entityId}; animation playback skipped.");
                return false;
            }

            if (view.animConfig == null)
            {
                WarnOnce($"anim-config-null:{entityId}", $"[PlayerAnimViewSystem] animConfig == null: entity={entityId}; animation playback skipped.");
                return false;
            }

            return true;
        }

        private void PlayOrWarn(AnimancerComponent animancer, TransitionAsset asset, int entityId, PlayerLogicState state, string fieldName)
        {
            if (asset != null)
            {
                animancer.Play(asset);
                return;
            }

            WarnMissingTransition(entityId, state, fieldName);
        }

        private void WarnMissingTransition(int entityId, PlayerLogicState state, string fieldName)
        {
            WarnOnce(
                $"transition-null:{entityId}:{state}:{fieldName}",
                $"[PlayerAnimViewSystem] Missing PlayerAnimConfig transition: entity={entityId}, state={state}, field={fieldName}.");
        }

        private void WarnOnce(string key, string message)
        {
            if (_warningKeys.Add(key))
            {
                Log.Warning(message);
            }
        }
    }
}
