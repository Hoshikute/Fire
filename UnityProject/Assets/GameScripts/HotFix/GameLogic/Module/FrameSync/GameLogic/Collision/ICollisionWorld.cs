
namespace GameLogic
{
    /// <summary>
    /// 确定性碰撞世界接口（逻辑层服务）。
    /// 帧同步逻辑层禁止使用 Physics.Raycast / CapsuleCast（非确定性、跨端不一致），
    /// 所有碰撞检测必须通过本接口的确定性实现。
    ///
    /// 当前阶段（P2-4）提供 AABB/胶囊 vs 静态几何的基础查询。
    /// 后续可扩展为 BVH/网格碰撞。
    ///
    /// 全程定点整数（与 SyncVector3 同量纲，SCALE=1000），不得经过 float。
    /// </summary>
    public interface ICollisionWorld
    {
        /// <summary>
        /// 胶囊扫掠：从 from 扫掠到 to，检测是否与静态几何碰撞。
        /// 参数全部为定点整数。
        /// </summary>
        /// <param name="from">扫掠起点（定点）</param>
        /// <param name="to">扫掠终点（定点）</param>
        /// <param name="radius">胶囊半径（定点，毫单位）</param>
        /// <param name="height">胶囊高度（定点，毫单位，不含两端半球）</param>
        CollisionResult CapsuleSweep(SyncVector3 from, SyncVector3 to, int radius, int height);

        /// <summary>
        /// 胶囊重叠检测：检测胶囊在指定位置是否与静态几何重叠。
        /// </summary>
        bool CapsuleOverlap(SyncVector3 center, int radius, int height);

        /// <summary>
        /// 添加静态碰撞体（AABB）。
        /// </summary>
        void AddBox(SyncVector3 min, SyncVector3 max);

        /// <summary>
        /// 清空所有静态碰撞体。
        /// </summary>
        void Clear();
    }
}
