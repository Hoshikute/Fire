using TEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家空闲状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerIdleState : PlayerMovementFsmState
    {
        private PlayerIdleData idleData;

        protected internal override void OnInit(IFsm<Player> fsm)
        {
            base.OnInit(fsm);
            idleData = playerSO.playerMovementData.PlayerIdleData;
        }

        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);
            reusableData.currentCrouchIdleIndex = -1;
            reusableData.currentStandIdleIndex = -1;
            reusableLogic.InitIldeState();
            reusableLogic.PlayNextState();
        }

        protected override void AddEventListening()
        {
            base.AddEventListening();
            inputServer.inputMap.Player.Move.started += MoveStart;
            inputServer.inputMap.Player.Jump.started += OnJumpStart;
            inputServer.inputMap.Player.Crouch.started += OnCrouch;
            player.IsOnGround.ValueChanged += OnCheckFall;
            reusableData.lockValueParameter.Parameter.OnValueChanged += LockValueChange;
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            inputServer.inputMap.Player.Move.started -= MoveStart;
            inputServer.inputMap.Player.Jump.started -= OnJumpStart;
            inputServer.inputMap.Player.Crouch.started -= OnCrouch;
            player.IsOnGround.ValueChanged -= OnCheckFall;
            reusableData.lockValueParameter.Parameter.OnValueChanged -= LockValueChange;
        }

        private void LockValueChange(float obj)
        {
            if (obj == 1 || obj == 0)
            {
                SwitchState<PlayerIdleState>();
            }
        }

        private void MoveStart(InputAction.CallbackContext context)
        {
            SwitchState<PlayerMoveStartState>();
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);
            UpdateCashVelocity(player.AnimationVelocity);
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
