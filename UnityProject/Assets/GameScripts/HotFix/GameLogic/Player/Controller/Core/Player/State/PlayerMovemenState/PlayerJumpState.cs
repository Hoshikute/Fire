using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家跳跃状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerJumpState : PlayerMovementFsmState
    {
        private PlayerJumpFallAndLandData jumpFallAndLandData;

        protected internal override void OnInit(IFsm<Player> fsm)
        {
            base.OnInit(fsm);
            jumpFallAndLandData = playerSO.playerMovementData.PlayerJumpFallAndLandData;
        }

        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);
            reusableData.currentInertialVelocity = GetInertialVelocity();
            Debug.Log($"[JumpDebug] EXEC: JumpState entered | inertialSpeed={reusableData.currentInertialVelocity.magnitude / Time.deltaTime:F2} isPlaceJump={GameModule.Input.Move == Vector2.zero}");
            reusableData.currentInertialVelocity.y = 0;

            player.ChangeVerticalSpeed(ToolFunction.GetJumpInitVelocity(0.8f, player.gravity));
            player.IgnoreRootMotionY = false;

            if (GameModule.Input.Move == Vector2.zero)
            {
                animancer.Play(jumpFallAndLandData.placeJumpStart).Events(player).OnEnd = OnEnterFall;
                reusableData.isInPlaceJump = true;
            }
            else
            {
                animancer.Play(jumpFallAndLandData.forwardJumpStart).Events(player).OnEnd = OnEnterFall;
                reusableData.isInPlaceJump = false;
            }
        }

        protected override void AddEventListening()
        {
            base.AddEventListening();
            player.IsOnGround.ValueChanged += OnFallToLand;
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            player.IsOnGround.ValueChanged -= OnFallToLand;
            reusableData.inputInterruptionCB = null;
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);
            InAirMove();
            UpdateRotation(false, 0, true, 2);
        }

        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            player.IgnoreRootMotionY = false;
        }

        private void OnEnterFall()
        {
            SwitchState<PlayerFallLoopState>();
        }

        private void OnFallToLand(bool onGround)
        {
            if (onGround)
            {
                SwitchState<PlayerLandState>();
            }
        }
    }
}