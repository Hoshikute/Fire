
namespace GameLogic
{
    /// <summary>
    /// 玩家移动组件（可回滚的「时刻组件」）。
    /// 持有确定性的位置 / 朝向 / 速度 / 接地状态，全部用定点数（int / SyncVector3）。
    /// 因为参与预测回滚，必须继承 MomentComponentBase 并实现真正的深拷贝：
    /// SyncVector3 是值类型但内部仍调用 DeepCopy() 保持范式一致，
    /// 任何引用类型字段（若以后新增）都必须新建对象，否则回滚后状态污染（见 pitfalls/rollback-bugs.md）。
    /// </summary>
    public class PlayerMoveComponent : MomentComponentBase
    {
        /// <summary>当前位置（定点）。</summary>
        public SyncVector3 pos = SyncVector3.Zero;

        /// <summary>当前朝向（定点单位向量）。</summary>
        public SyncVector3 faceDir = SyncVector3.FromRaw(0, 0, SyncVector3.ONE);

        /// <summary>
        /// 本逻辑帧的移动意图方向（来自帧指令，定点单位向量）。
        /// 供表现层按实体驱动方向动画，不直接读取全局输入单例。
        /// </summary>
        public SyncVector3 moveIntentDir = SyncVector3.Zero;

        /// <summary>本逻辑帧速度档位（1 = 走，2 = 跑），来自帧指令。</summary>
        public int speedGear = 1;

        /// <summary>水平移动速度（定点，单位：毫单位 / 秒，与 SyncVector3 同量纲）。</summary>
        public int moveSpeed = 4000;

        /// <summary>竖直速度（定点，正为向上）。用于跳跃 / 下落。</summary>
        public int verticalSpeed = 0;

        /// <summary>是否接地。</summary>
        public bool isOnGround = true;

        /// <summary>胶囊碰撞体半径（定点，毫单位）。默认 300（0.3m）。</summary>
        public int capsuleRadius = 300;

        /// <summary>胶囊碰撞体高度（定点，毫单位，不含两端半球）。默认 1200（1.2m）。</summary>
        public int capsuleHeight = 1200;

        /// <summary>
        /// 当前实际移动速度（定点，毫单位/秒）。
        /// 受加减速曲线影响，不同于标称 WalkSpeed/RunSpeed。
        /// </summary>
        public int currentSpeed = 0;

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
}
