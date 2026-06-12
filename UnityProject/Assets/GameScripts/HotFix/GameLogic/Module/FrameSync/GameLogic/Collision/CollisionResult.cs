
namespace GameLogic
{
    /// <summary>
    /// 确定性碰撞查询结果。
    /// 所有数据均为定点整数，跨端/回滚重算完全一致。
    /// </summary>
    public struct CollisionResult
    {
        /// <summary>是否发生碰撞。</summary>
        public bool hit;

        /// <summary>碰撞面法线（定点单位向量，|normal| = SyncVector3.ONE）。</summary>
        public SyncVector3 normal;

        /// <summary>穿透深度（定点，毫单位）。正值表示需要沿法线方向推出。</summary>
        public int penetration;

        /// <summary>碰撞点（定点）。</summary>
        public SyncVector3 point;

        public static CollisionResult None => new CollisionResult
        {
            hit = false,
            normal = SyncVector3.FromRaw(0, SyncVector3.ONE, 0),
            penetration = 0,
            point = SyncVector3.Zero,
        };
    }
}
