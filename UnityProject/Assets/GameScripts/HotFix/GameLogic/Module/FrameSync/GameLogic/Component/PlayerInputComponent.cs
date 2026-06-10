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
        /// 已包含相机朝向修正（在 PlayerInputCollectSystem 中完成）。
        /// </summary>
        public SyncVector3 moveDir = SyncVector3.Zero;

        /// <summary>本帧是否按下跳跃（边沿触发，消费后清空）。</summary>
        public bool jump;

        /// <summary>本帧是否切换锁定模式（边沿触发，消费后清空）。</summary>
        public bool toggleLock;

        /// <summary>本帧是否触发平台跳请求（边沿触发，消费后清空）。</summary>
        public bool platformJump;

        /// <summary>
        /// 速度档位（1 = 走，2 = 跑）。
        /// 由输入采集系统根据 Shift 键写入，PlayerMoveSystem 读取选择对应速度常量。
        /// 放在单例组件（不参与回滚），避免渲染帧直写回滚组件污染快照。
        /// </summary>
        public int speedGear = 1;

        /// <summary>
        /// 把所有边沿触发型输入清空。
        /// 逻辑帧消费完后调用，避免一次按键在多帧重复触发。
        /// 注意：moveDir 不清空——移动是持续性输入，松开摇杆时表现层会写回 Zero。
        /// </summary>
        public void ConsumeOneShot()
        {
            jump = false;
            toggleLock = false;
            platformJump = false;
        }
    }
}
