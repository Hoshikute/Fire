using Animancer;
using TEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家着陆状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerLandState : PlayerMovementFsmState
    {
        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);

            if (player.IsOnGround.Value)
            {
                AnimancerState state = null;
                int index = 0;

                if (player.VerticalSpeed < -15)
                {
                    index = 1;
                }

                if (!reusableData.isInPlaceJump)
                {
                    if (playerSO.playerMovementData.PlayerJumpFallAndLandData.forwardJumpLand.Length == 1)
                    {
                        index = 0;
                    }
                    state = animancer.Play(playerSO.playerMovementData.PlayerJumpFallAndLandData.forwardJumpLand[index]);
                }
                else
                {
                    if (playerSO.playerMovementData.PlayerJumpFallAndLandData.placeJumpLand.Length == 1)
                    {
                        index = 0;
                    }
                    state = animancer.Play(playerSO.playerMovementData.PlayerJumpFallAndLandData.placeJumpLand[index]);
                }

                state.Events(player).SetCallback(playerSO.playerParameterData.moveInterruptEvent, () => OnInputInterruption(currentFsm));
                state.Events(player).OnEnd = () => SwitchState<PlayerIdleState>();
            }
            else
            {
                SwitchState<PlayerIdleState>();
            }
        }
    }
}
