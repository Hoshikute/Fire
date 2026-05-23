using TEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家空闲状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerIdleState : PlayerMovementFsmState
    {
        private PlayerIdleData idleData;
        private int _fallCheckTid;

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
            player.IsOnGround.ValueChanged += OnCheckFall;
            reusableData.lockValueParameter.Parameter.OnValueChanged += LockValueChange;
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            player.IsOnGround.ValueChanged -= OnCheckFall;
            reusableData.lockValueParameter.Parameter.OnValueChanged -= LockValueChange;
            if (_fallCheckTid != 0)
            {
                GameModule.Timer.RemoveTimer(_fallCheckTid);
                _fallCheckTid = 0;
            }
        }

        private void LockValueChange(float obj)
        {
            if (obj == 1 || obj == 0)
            {
                SwitchState<PlayerIdleState>();
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

            if (GameModule.Input.Move != UnityEngine.Vector2.zero)
            {
                SwitchState<PlayerMoveStartState>();
                return;
            }

            UpdateCashVelocity(player.AnimationVelocity);
            UpdateSpeed();
        }

        private void OnCheckFall(bool isGround)
        {
            if (!isGround)
            {
                _fallCheckTid = GameModule.Timer.AddTimer((timer) =>
                {
                    if (!player.IsOnGround.Value)
                    {
                        SwitchState<PlayerFallLoopState>();
                    }
                }, time: 0.05f);
            }
        }
    }
}
