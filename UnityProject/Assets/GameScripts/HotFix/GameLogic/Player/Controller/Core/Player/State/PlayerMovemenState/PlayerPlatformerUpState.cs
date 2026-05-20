using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家平台跳跃状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerPlatformerUpState : PlayerMovementFsmState
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
            player.ChangeVerticalSpeed(ToolFunction.GetJumpInitVelocity(reusableData.jumpExternalForce, player.gravity));
            player.IgnoreRootMotionY = true;
            animancer.Play(jumpFallAndLandData.platformerUpStart).Events(player).OnEnd = OnUpStartEnd;
            reusableData.currentInertialVelocity = UnityEngine.Vector3.zero;
            reusableData.currentMidInAirMultiplier = 2;
            reusableData.isInPlaceJump = false;
        }

        private void OnUpStartEnd()
        {
            animancer.Play(jumpFallAndLandData.platformerUpLoop);
        }

        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            player.IgnoreRootMotionY = false;
            reusableData.currentMidInAirMultiplier = 0.6f;
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);
            if (player.VerticalSpeed < 0)
            {
                animancer.Play(jumpFallAndLandData.platFormerDownLoop);
            }
            reusableLogic.InAirMoveCheck(GetTargetDir());
            InAirMove();
            UpdateRotation(false, 0, true, 2.2f);
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

        private void OnFallToLand(bool onGround)
        {
            if (onGround)
            {
                SwitchState<PlayerLandState>();
            }
        }
    }
}
