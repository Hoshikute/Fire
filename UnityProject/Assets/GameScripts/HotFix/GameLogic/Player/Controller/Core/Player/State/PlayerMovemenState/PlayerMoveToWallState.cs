using TEngine;
using UnityEngine;

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
        }

        protected override void RemoveEventListening()
        {
            base.RemoveEventListening();
        }

        protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            if (GameModule.Input.Move == Vector2.zero)
            {
                return;
            }

            Vector3 dir = GetTargetDir();
            if (Physics.Raycast(player.transform.position + Vector3.up, dir, 1, player.whatIsGround))
            {
                return;
            }

            SwitchState<PlayerMoveStartState>();
        }
    }
}
