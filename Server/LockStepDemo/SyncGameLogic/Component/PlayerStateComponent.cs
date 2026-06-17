public enum PlayerLogicState
{
    Idle = 0,
    MoveStart = 1,
    MoveLoop = 2,
    MoveEnd = 3,
    Jump = 4,
    JumpInPlace = 5,
    Fall = 6,
    Land = 7,
    LockIdle = 8,
    MoveToWall = 9,
    Vault = 10,
    Climb = 11,
    LedgeClimb = 12,
    PlatformerUp = 13,
}

public class PlayerStateComponent : MomentComponentBase
{
    public PlayerLogicState state = PlayerLogicState.Idle;
    public int framesInState = 0;
    public PlayerLogicState prevState = PlayerLogicState.Idle;
    public bool isLocked = false;
    public bool platformJumpRequested = false;
    public int wallObstructType = 0;
    public bool isInPlaceJump = false;

    public override MomentComponentBase DeepCopy()
    {
        PlayerStateComponent c = new PlayerStateComponent();
        c.ID = ID;
        c.Frame = Frame;
        c.state = state;
        c.framesInState = framesInState;
        c.prevState = prevState;
        c.isLocked = isLocked;
        c.platformJumpRequested = platformJumpRequested;
        c.wallObstructType = wallObstructType;
        c.isInPlaceJump = isInPlaceJump;
        return c;
    }
}
