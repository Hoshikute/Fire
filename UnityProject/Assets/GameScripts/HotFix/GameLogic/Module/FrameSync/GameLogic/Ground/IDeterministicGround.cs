
namespace GameLogic
{
    /// <summary>
    /// 确定性地面查询接口（逻辑层服务）。
    /// 帧同步逻辑层禁止使用 Physics.Raycast / CheckSphere（非确定性、跨端不一致），
    /// 所有"脚下地面在哪"的查询都必须走本接口的确定性实现。
    ///
    /// 设计为接口是为了后续分轮扩展：
    ///   - 第1轮 FlatGround：平地高度场（等价老的 y&lt;=0 占位，但可替换）。
    ///   - 第3轮 斜坡：返回真实地面高度 + 法线。
    ///   - 第4轮 碰撞：基于确定性静态几何（格子/网格）查询。
    ///
    /// 全程定点整数（与 SyncVector3 同量纲，SCALE=1000），不得经过 float。
    /// </summary>
    public interface IDeterministicGround
    {
        /// <summary>
        /// 查询给定水平位置 (x, z) 处的地面高度 y（定点，毫单位）。
        /// 入参 x/z 为定点整数；返回该列地面的 y（定点）。
        /// </summary>
        int SampleHeight(int x, int z);

        /// <summary>
        /// 查询给定水平位置处的地面法线（定点单位向量，|normal| = SyncVector3.ONE = 1000）。
        /// 平地返回 (0, ONE, 0)，斜坡返回斜面法线。
        /// </summary>
        SyncVector3 GetNormal(int x, int z);

        /// <summary>
        /// 判断给定位置是否接地：实体当前 y 是否已落到该列地面高度或以下（带一个微小容差）。
        /// </summary>
        bool IsGrounded(SyncVector3 pos, int toleranceFixed = 0);
    }
}
