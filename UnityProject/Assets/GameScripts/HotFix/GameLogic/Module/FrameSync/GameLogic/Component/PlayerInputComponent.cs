// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

namespace GameLogic
{
    /// <summary>
    /// 玩家输入组件（单例）。
    /// 表现层（渲染帧）采集 Unity 输入写入这里，逻辑层（逻辑帧）读取它驱动确定性移动。
    /// 这是「表现 → 逻辑」唯一允许的写入通道：输入意图本质是玩家指令，
    /// 真正的状态推进仍由逻辑层在固定 200ms 逻辑帧里完成。
    /// 方向用 SyncVector3 定点数，禁止在逻辑里用 float 参与运算。
    /// </summary>
    public class PlayerInputComponent : SingletonComponent
    {
        /// <summary>
        /// 移动方向（定点单位向量，长度≈SyncVector3.ONE）。
        /// 由表现层把 Unity 的 Vector2 输入转成定点并归一化后写入。
        /// </summary>
        public SyncVector3 moveDir = SyncVector3.Zero;

        /// <summary>本帧是否按下跳跃。</summary>
        public bool jump;

        /// <summary>
        /// 把输入清空（逻辑帧消费后调用，避免一次输入被多帧重复执行）。
        /// 注意：moveDir 不清空——移动是持续性输入，松开手柄时表现层会写回 Zero。
        /// jump 是边沿触发，消费后必须清掉。
        /// </summary>
        public void ConsumeOneShot()
        {
            jump = false;
        }
    }
}
