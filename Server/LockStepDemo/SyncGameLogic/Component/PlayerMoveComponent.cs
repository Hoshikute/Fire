using UnityEngine;

public class PlayerMoveComponent : MomentComponentBase
{
    public SyncVector3 pos = new SyncVector3();
    public SyncVector3 faceDir = new SyncVector3();
    public SyncVector3 moveIntentDir = new SyncVector3();
    public int speedGear = 1;
    public int moveSpeed = 4000;
    public int verticalSpeed = 0;
    public bool isOnGround = true;
    public int capsuleRadius = 300;
    public int capsuleHeight = 1200;
    public int currentSpeed = 0;

    public PlayerMoveComponent()
    {
        faceDir.z = 1000;
    }

    public override MomentComponentBase DeepCopy()
    {
        PlayerMoveComponent c = new PlayerMoveComponent();
        c.ID = ID;
        c.Frame = Frame;
        c.pos = pos.DeepCopy();
        c.faceDir = faceDir.DeepCopy();
        c.moveIntentDir = moveIntentDir.DeepCopy();
        c.speedGear = speedGear;
        c.moveSpeed = moveSpeed;
        c.verticalSpeed = verticalSpeed;
        c.isOnGround = isOnGround;
        c.capsuleRadius = capsuleRadius;
        c.capsuleHeight = capsuleHeight;
        c.currentSpeed = currentSpeed;
        return c;
    }
}
