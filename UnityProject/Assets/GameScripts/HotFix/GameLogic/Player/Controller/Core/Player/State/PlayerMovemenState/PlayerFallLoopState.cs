using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家下落循环状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerFallLoopState : PlayerMovementFsmState
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
            player.IgnoreRootMotionY = false;
            animancer.Play(jumpFallAndLandData.fallStart).Events(player).OnEnd = OnFallLoop;
        }

        private void OnFallLoop()
        {
            animancer.Play(jumpFallAndLandData.fall);
        }

        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            player.IgnoreRootMotionY = false;
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);
            reusableLogic.InAirMoveCheck(GetTargetDir());
            InAirMove();
            UpdateRotation(false, 0, true, 2);
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
