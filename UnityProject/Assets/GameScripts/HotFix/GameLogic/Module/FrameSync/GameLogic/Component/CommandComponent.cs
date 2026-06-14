namespace GameLogic
{
    /// <summary>
    /// 命令组件
    /// </summary>
    public class CommandComponent : PlayerCommandBase
    {
        public SyncVector3 moveDir = new SyncVector3();
        public SyncVector3 skillDir = new SyncVector3();

        public bool jump;
        public bool toggleLock;
        public bool platformJump;
        public int speedGear = 1;

        public int element1;
        public int element2;
        public bool isFire = false;

        public void ClearOneShotInputs()
        {
            jump = false;
            toggleLock = false;
            platformJump = false;
        }

        public override PlayerCommandBase DeepCopy()
        {
            CommandComponent cc = new CommandComponent();
            cc.id = id;
            cc.frame = frame;
            cc.time = time;
            cc.isFire = isFire;
            cc.moveDir = moveDir.DeepCopy();
            cc.skillDir = skillDir.DeepCopy();
            cc.jump = jump;
            cc.toggleLock = toggleLock;
            cc.platformJump = platformJump;
            cc.speedGear = speedGear;
            cc.element1 = element1;
            cc.element2 = element2;
            return cc;
        }

        public override bool EqualsCmd(PlayerCommandBase cmd)
        {
            if (!(cmd is CommandComponent cc))
                return false;

            if (id != cc.id) return false;
            if (frame != cc.frame) return false;
            if (isFire != cc.isFire) return false;
            if (jump != cc.jump) return false;
            if (toggleLock != cc.toggleLock) return false;
            if (platformJump != cc.platformJump) return false;
            if (speedGear != cc.speedGear) return false;
            if (element1 != cc.element1) return false;
            if (element2 != cc.element2) return false;
            if (!moveDir.Equals(cc.moveDir)) return false;
            if (!skillDir.Equals(cc.skillDir)) return false;

            return true;
        }
    }
}
