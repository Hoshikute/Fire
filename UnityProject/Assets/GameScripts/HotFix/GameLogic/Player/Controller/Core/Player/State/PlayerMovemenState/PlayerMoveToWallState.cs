using TEngine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家移动到墙边状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerMoveToWallState : PlayerMovementFsmState
    {
        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);
            animancer.Play(playerSO.playerMovementData.PlayerMoveEndData.moveToWall);
        }

        protected override void AddEventListening()
        {
            base.AddEventListening();
            inputServer.inputMap.Player.Move.started += OnMove;
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
            inputServer.inputMap.Player.Move.started -= OnMove;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Vector3 dir = GetTargetDir();
            if (Physics.Raycast(player.transform.position + Vector3.up, dir, 1, player.whatIsGround))
            {
                return;
            }
            SwitchState<PlayerMoveStartState>();
        }
    }
}
