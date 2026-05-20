using TEngine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家移动循环状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerMoveLoopState : PlayerMovementFsmState
    {
        private PlayerMoveLoopData moveLoopData;
        private int tid;

        protected internal override void OnInit(IFsm<Player> fsm)
        {
            base.OnInit(fsm);
            moveLoopData = playerSO.playerMovementData.PlayerMoveLoopData;
        }

        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);
            animancer.Play(moveLoopData.moveLoop);
            OnCheckInput();
            reusableData.rotationValueParameter.CurrentValue = 0;
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);
            UpdateCashVelocity(player.AnimationVelocity);

            if (reusableData.lockValueParameter.TargetValue == 1)
            {
                UpdateRotation(true, 0.5f, false);
            }
            else
            {
                UpdateRotation(true, 0.4f, true, 1.4f);
            }
            UpdateSpeed();

            if (reusableData.speedValueParameter.CurrentValue <= 1)
            {
                reusableData.checkWallDistance = 0.6f;
            }
            else
            {
                reusableData.checkWallDistance = 0.4f * reusableData.speedValueParameter.CurrentValue;
            }

            Debug.DrawLine(player.transform.position + Vector3.up, player.transform.position + Vector3.up + player.transform.forward * reusableData.checkWallDistance, Color.yellow, 0.05f);
            if (Physics.Raycast(player.transform.position + Vector3.up, player.transform.forward, out var hitInfo, reusableData.checkWallDistance, player.whatIsGround))
            {
                if (Mathf.Abs(ToolFunction.GetDeltaAngle(player.transform.forward, -hitInfo.normal)) < 40)
                {
                    SwitchState<PlayerMoveEndState>();
                }
            }
        }

        protected override void AddEventListening()
        {
            base.AddEventListening();
            inputServer.inputMap.Player.Jump.started += OnJumpStart;
            inputServer.inputMap.Player.Move.canceled += OnCheckMoveEnd;
            inputServer.inputMap.Player.Crouch.started += OnCrouch;
            player.IsOnGround.ValueChanged += OnCheckFall;
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            inputServer.inputMap.Player.Jump.started -= OnJumpStart;
            inputServer.inputMap.Player.Move.canceled -= OnCheckMoveEnd;
            inputServer.inputMap.Player.Crouch.started -= OnCrouch;
            player.IsOnGround.ValueChanged -= OnCheckFall;
        }

        protected internal override void OnLeave(IFsm<Player> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            timerService.RemoveTimer(tid);
        }

        private void OnCheckMoveEnd(InputAction.CallbackContext context)
        {
            OnCheckInput();
        }

        private void OnCheckInput()
        {
            if (inputServer.Move != UnityEngine.Vector2.zero)
            {
                return;
            }
            SwitchState<PlayerMoveEndState>();
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
