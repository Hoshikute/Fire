using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class FrameSyncComponentModelTests
{
    private class TestWorld : WorldBase
    {
    }

    [Test]
    public void CreateEntityInfo_SerializesSharedComponentsOnly()
    {
        TestWorld world = new TestWorld();
        world.Init(false);

        EntityBase entity = world.CreateEntity(
            101,
            new PlayerComponent { playerId = 101, playerName = "A", isLocal = false },
            new PlayerMoveComponent(),
            new PlayerStateComponent(),
            new CommandComponent(),
            new MoveComponent(),
            new TransfromComponent());

        ServiceSyncSystem syncSystem = new ServiceSyncSystem();
        syncSystem.m_world = world;

        EntityInfo info = syncSystem.CreateEntityInfo(entity, null);
        HashSet<string> names = new HashSet<string>(info.infos.Select(item => item.m_compName));

        Assert.That(names.Contains("PlayerComponent"), Is.True);
        Assert.That(names.Contains("PlayerMoveComponent"), Is.True);
        Assert.That(names.Contains("PlayerStateComponent"), Is.True);
        Assert.That(names.Contains("CommandComponent"), Is.True);
        Assert.That(names.Contains("MoveComponent"), Is.False);
        Assert.That(names.Contains("TransfromComponent"), Is.False);
    }

    [Test]
    public void CreateEntityInfo_IncludesExistingPlayerStateForLateJoinSnapshot()
    {
        TestWorld world = new TestWorld();
        world.Init(false);

        PlayerMoveComponent move = new PlayerMoveComponent();
        move.pos.x = 123000;
        move.pos.z = 456000;

        EntityBase existingPlayer = world.CreateEntity(
            201,
            new PlayerComponent { playerId = 201, playerName = "Existing", isLocal = false },
            move,
            new PlayerStateComponent(),
            new CommandComponent(),
            new MoveComponent());

        ServiceSyncSystem syncSystem = new ServiceSyncSystem();
        syncSystem.m_world = world;

        EntityInfo info = syncSystem.CreateEntityInfo(existingPlayer, null);
        ComponentInfo moveInfo = info.infos.Single(item => item.m_compName == "PlayerMoveComponent");

        StringAssert.Contains("123000", moveInfo.content);
        StringAssert.Contains("456000", moveInfo.content);
        Assert.That(info.infos.Any(item => item.m_compName == "MoveComponent"), Is.False);
    }

    [Test]
    public void PlayerMoveDeepCopy_CopiesSharedFields()
    {
        PlayerMoveComponent move = new PlayerMoveComponent();
        move.ID = 11;
        move.Frame = 22;
        move.pos.x = 100;
        move.faceDir.z = 1000;
        move.moveIntentDir.x = 500;
        move.speedGear = 2;
        move.moveSpeed = 8000;
        move.verticalSpeed = 1200;
        move.isOnGround = false;
        move.capsuleRadius = 350;
        move.capsuleHeight = 1300;
        move.currentSpeed = 7000;

        PlayerMoveComponent copy = (PlayerMoveComponent)move.DeepCopy();

        Assert.That(copy.ID, Is.EqualTo(move.ID));
        Assert.That(copy.Frame, Is.EqualTo(move.Frame));
        Assert.That(copy.pos.x, Is.EqualTo(move.pos.x));
        Assert.That(copy.faceDir.z, Is.EqualTo(move.faceDir.z));
        Assert.That(copy.moveIntentDir.x, Is.EqualTo(move.moveIntentDir.x));
        Assert.That(copy.speedGear, Is.EqualTo(move.speedGear));
        Assert.That(copy.moveSpeed, Is.EqualTo(move.moveSpeed));
        Assert.That(copy.verticalSpeed, Is.EqualTo(move.verticalSpeed));
        Assert.That(copy.isOnGround, Is.EqualTo(move.isOnGround));
        Assert.That(copy.capsuleRadius, Is.EqualTo(move.capsuleRadius));
        Assert.That(copy.capsuleHeight, Is.EqualTo(move.capsuleHeight));
        Assert.That(copy.currentSpeed, Is.EqualTo(move.currentSpeed));
    }

    [Test]
    public void SyncEntityMsg_CarriesJoinSnapshotMetadata()
    {
        SyncEntityMsg msg = new SyncEntityMsg
        {
            frame = 10,
            snapshotId = 20,
            snapshotFrame = 30,
            selfEntityId = 40,
            createEntityIndex = 50,
            intervalTime = 60,
            advanceCount = 1,
            isSnapshot = true,
            isSnapshotComplete = true,
            infos = new List<EntityInfo>(),
            destroyList = new List<int>(),
        };

        Assert.That(msg.isSnapshot, Is.True);
        Assert.That(msg.isSnapshotComplete, Is.True);
        Assert.That(msg.snapshotId, Is.EqualTo(20));
        Assert.That(msg.selfEntityId, Is.EqualTo(40));
        Assert.That(msg.intervalTime, Is.EqualTo(60));
    }
}
