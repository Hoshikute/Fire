using TEngine;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家锁定空闲状态 - TEngine FSM 版本。
    /// 处理锁定状态下的空闲输入：跳跃、蹲伏、移动。
    /// </summary>
    public class PlayerLockIdleState : PlayerMovementFsmState
    {
        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);
            reusableData.currentCrouchIdleIndex = -1;
            reusableData.currentStandIdleIndex = -1;
            reusableLogic.InitIldeState();
            reusableLogic.PlayNextState();
            CheckCurrentFall();
        }

        protected override void AddEventListening()
        {
            base.AddEventListening();
            player.IsOnGround.ValueChanged += OnCheckFall;
            reusableData.lockValueParameter.Parameter.OnValueChanged += LockValueChange;
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            player.IsOnGround.ValueChanged -= OnCheckFall;
            reusableData.lockValueParameter.Parameter.OnValueChanged -= LockValueChange;
        }

        private void LockValueChange(float obj)
        {
            if (obj == 1 || obj == 0)
            {
                SwitchState<PlayerLockIdleState>();
            }
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            if (GameModule.Input.GetButtonDown(InputButtonType.Jump))
            {
                OnJumpStart();
                return;
            }

            if (GameModule.Input.GetButtonDown(InputButtonType.Crouch))
            {
                OnCrouch();
            }

            if (GameModule.Input.Move != Vector2.zero)
            {
                SwitchState<PlayerMoveStartState>();
                return;
            }

            UpdateCashVelocity(player.AnimationVelocity);
            UpdateSpeed();
        }
    }
}