using System.Collections;
using System.Collections.Generic;

using UnityEngine;
/**************************************************************************
����: HuHu
����: 3112891874@qq.com
����: ����״̬��������״̬����������״̬��
**************************************************************************/
public class PlayerStateMachine : StateMachineBase
{
   //����״̬
    public Player player;
    public PlayerIdleState idleState;
    public PlayerMoveStartState moveStartState;
    public PlayerMoveLoopState moveLoopState;
    public PlayerMoveEndState moveEndState;
    public PlayerJumpState jumpState;
    public PlayerClimbState climbState;
    public PlayerLedgeClimbState ledgeClimbState;
    public PlayerMoveToWallState moveWallState;
    public PlayerFallLoopState fallLoopState;
    public PlayerPlatformerUpState platformerUpState;
    public PlayerLandState landState;
    public PlayerStateMachine(Player player)
    {
        this.player = player;
        idleState = new PlayerIdleState(this);
        moveStartState = new PlayerMoveStartState(this);
        moveLoopState = new PlayerMoveLoopState(this);
        moveEndState = new PlayerMoveEndState(this);
        jumpState= new PlayerJumpState(this);
        climbState = new PlayerClimbState(this);
        ledgeClimbState = new PlayerLedgeClimbState(this);
        moveWallState = new  PlayerMoveToWallState(this);
        fallLoopState = new PlayerFallLoopState(this);
        platformerUpState = new PlayerPlatformerUpState(this);
        landState= new PlayerLandState(this);
    }
    public override void ChangeState(IState targetState)
    {
        base.ChangeState(targetState);
        player.ReusableData.currentState.Value = targetState.GetType().Name;
    }
}
