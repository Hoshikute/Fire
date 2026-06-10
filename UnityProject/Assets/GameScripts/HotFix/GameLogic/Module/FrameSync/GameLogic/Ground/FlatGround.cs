// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

namespace GameLogic
{
    /// <summary>
    /// 平地地面实现：地面是一个恒定高度的水平面（默认 y=0）。
    /// 等价于老 PlayerMoveSystem 里硬编码的 "y &lt;= 0 视为接地"，
    /// 但抽到接口背后，后续可无缝替换为斜坡 / 碰撞几何实现而不动 PlayerMoveSystem。
    ///
    /// 无状态、纯函数式：跨端 / 回滚重算结果恒定一致。
    /// </summary>
    public sealed class FlatGround : IDeterministicGround
    {
        /// <summary>地面高度（定点，毫单位）。默认 0。</summary>
        private readonly int m_groundHeight;

        public FlatGround(int groundHeightFixed = 0)
        {
            m_groundHeight = groundHeightFixed;
        }

        public int SampleHeight(int x, int z)
        {
            // 平地：任何 (x,z) 列地面高度都相同。
            return m_groundHeight;
        }

        public bool IsGrounded(SyncVector3 pos, int toleranceFixed = 0)
        {
            // pos.y 落到地面高度（含容差）或以下即接地。
            return pos.y <= m_groundHeight + toleranceFixed;
        }
    }
}
