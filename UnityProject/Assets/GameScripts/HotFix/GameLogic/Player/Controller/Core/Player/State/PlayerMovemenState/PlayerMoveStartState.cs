using Animancer;
using TEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家起步状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerMoveStartState : PlayerMovementFsmState
    {
        private const float MoveLoopTransitionNormalizedTime = 0.85f;
        private PlayerMoveStartData moveStartData;
        private float targetAngle;
        private bool isForwardMove;
        private AnimancerState state;

        protected internal override void OnInit(IFsm<Player> fsm)
        {
            base.OnInit(fsm);
            moveStartData = playerSO.playerMovementData.PlayerMoveStartData;
        }

        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);

            if (reusableData.lockValueParameter.TargetValue == 1)
            {
                SwitchState<PlayerMoveLoopState>();
                return;
            }

            CheckCurrentFall();
            targetAngle = UpdateRotation();
            if (targetAngle < 22.5 && targetAngle >= 0 || targetAngle >= -22.5 && targetAngle <= 0)
            {
                state = animancer.Play(moveStartData.moveStart_F);
                isForwardMove = true;
            }
            else if (targetAngle >= 22.5 && targetAngle < 67.5)
            {
                state = animancer.Play(moveStartData.moveStart_R45);
            }
            else if (targetAngle >= 67.5 && targetAngle < 112.5)
            {
                state = animancer.Play(moveStartData.moveStart_R90);
            }
            else if (targetAngle >= 112.5 && targetAngle < 157.5)
            {
                state = animancer.Play(moveStartData.moveStart_R135);
            }
            else if (targetAngle >= 157.5 || targetAngle < -157.5)
            {
                state = animancer.Play(moveStartData.moveStart_R180);
            }
            else if (targetAngle >= -157.5 && targetAngle < -112.5)
            {
                state = animancer.Play(moveStartData.moveStart_L135);
            }
            else if (targetAngle >= -112.5 && targetAngle < -67.5)
            {
                state = animancer.Play(moveStartData.moveStart_L90);
            }
            else if (targetAngle >= -67.5 && targetAngle < -22.5)
            {
                state = animancer.Play(moveStartData.moveStart_L45);
            }
            state.Events(player).OnEnd = OnMoveStartEnd;
        }

        protected override void AddEventListening()
        {
            base.AddEventListening();
            player.IsOnGround.ValueChanged += OnCheckFall;
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            player.IsOnGround.ValueChanged -= OnCheckFall;
        }

        private bool OnCheckInput()
        {
            if (GameModule.Input.Move != UnityEngine.Vector2.zero)
            {
                return true;
            }
            SwitchState<PlayerMoveEndState>();
            return false;
        }

        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            if (state != null)
            {
                state.Events(player).OnEnd = null;
            }
            isForwardMove = false;
        }

        private void OnMoveStartEnd()
        {
            if (currentFsm.CurrentState != this)
            {
                return;
            }
            // 在 Animancer OnEnd 回调中使用延迟切换，避免在 PlayableGraph 评估期间修改拓扑导致卡顿
            DeferredSwitch<PlayerMoveLoopState>();
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            if (TryExecuteDeferredSwitch()) return;

            if (GameModule.Input.GetButtonDown(InputButtonType.Jump))
            {
                OnJumpStart();
                return;
            }

            if (GameModule.Input.GetButtonDown(InputButtonType.Crouch))
            {
                OnCrouch();
            }

            if (!OnCheckInput()) return;
            UpdateCashVelocity(player.AnimationVelocity);
            if (state.NormalizedTime > 0.4f || isForwardMove)
            {
                UpdateRotation(false, 0.7f, true, 1.8f);
            }
            UpdateSpeed();

            if (state.NormalizedTime >= MoveLoopTransitionNormalizedTime)
            {
                SwitchState<PlayerMoveLoopState>();
            }
        }
    }
}