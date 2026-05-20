using TEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家锁定空闲状态 - TEngine FSM 版本。
    /// </summary>
    public class PlayerLockIdleState : PlayerMovementFsmState
    {
        protected internal override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);
        }
    }
}
