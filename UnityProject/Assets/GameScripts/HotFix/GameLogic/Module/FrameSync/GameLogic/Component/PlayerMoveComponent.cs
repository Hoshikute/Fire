// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

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

        /// <summary>水平移动速度（定点，单位：毫单位 / 秒，与 SyncVector3 同量纲）。</summary>
        public int moveSpeed = 4000;

        /// <summary>竖直速度（定点，正为向上）。用于跳跃 / 下落。</summary>
        public int verticalSpeed = 0;

        /// <summary>是否接地。</summary>
        public bool isOnGround = true;

        public override MomentComponentBase DeepCopy()
        {
            PlayerMoveComponent c = new PlayerMoveComponent();
            c.ID = ID;
            c.Frame = Frame;
            c.pos = pos.DeepCopy();
            c.faceDir = faceDir.DeepCopy();
            c.moveSpeed = moveSpeed;
            c.verticalSpeed = verticalSpeed;
            c.isOnGround = isOnGround;
            return c;
        }
    }
}
