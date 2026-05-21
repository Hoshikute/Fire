using Animancer;
using TEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家起步状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerMoveStartState : PlayerMovementFsmState
    {
        private PlayerMoveStartData moveStartData;
        private float targetAngle;
        private bool isForwardMove;
        private int tid;
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

        private void OnCheckInput()
        {
            if (inputServer.Move != UnityEngine.Vector2.zero)
            {
                return;
            }
            SwitchState<PlayerMoveEndState>();
        }

        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            timerService.RemoveTimer(tid);
            isForwardMove = false;
        }

        private void OnMoveStartEnd()
        {
            SwitchState<PlayerMoveLoopState>();
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            if (inputServer.GetButtonDown(InputButtonType.Jump))
            {
                OnJumpStart();
                return;
            }

            if (inputServer.GetButtonDown(InputButtonType.Crouch))
            {
                OnCrouch();
            }

            OnCheckInput();
            UpdateCashVelocity(player.AnimationVelocity);
            if (state.NormalizedTime > 0.4f || isForwardMove)
            {
                UpdateRotation(false, 0.7f, true, 1.8f);
            }
            UpdateSpeed();
        }

        private void OnCheckFall(bool isGround)
        {
            if (!isGround)
            {
                timerService.AddTimer(50, () =>
                {
                    if (!player.IsOnGround.Value)
                    {
                        SwitchState<PlayerFallLoopState>();
                    }
                });
            }
        }
    }
}
