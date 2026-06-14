
namespace GameLogic
{
    /// <summary>
    /// 玩家输入组件（单例）。
    /// 当前本地路径由表现层（渲染帧）采集 Unity 输入写入这里，
    /// 逻辑帧开始时由 PlayerInputCommandSystem 固化为本地玩家实体的 CommandComponent。
    /// 这是本地控制下「表现 → 逻辑」唯一允许的写入通道：
    /// 这里只承载会进入帧指令 / 影响逻辑推进的输入意图，
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
        /// 由输入采集系统根据 Shift 键写入，再进入本地玩家实体的帧指令。
        /// 放在单例组件（不参与回滚），避免渲染帧直写回滚组件污染快照；
        /// CommandComponent 已承载此字段；Move/State 系统按实体帧指令读取，不直接读这里。
        /// </summary>
        public int speedGear = 1;

        /// <summary>
        /// 生成当前逻辑帧的玩家指令快照。
        /// time 由网络层/调用方填入；本地确定性路径不要在这里读取真实时间。
        /// </summary>
        public CommandComponent ToCommand(int frame, int id, int time)
        {
            CommandComponent command = new CommandComponent
            {
                frame = frame,
                id = id,
                time = time,
            };
            WriteToCommand(command);
            return command;
        }

        /// <summary>
        /// 将当前本地输入意图写入帧指令。
        /// 供后续接入权威输入/回滚时复用，避免影响逻辑的输入字段散落转换。
        /// </summary>
        public void WriteToCommand(CommandComponent command)
        {
            command.moveDir = moveDir.DeepCopy();
            command.jump = jump;
            command.toggleLock = toggleLock;
            command.platformJump = platformJump;
            command.speedGear = speedGear;
        }

        /// <summary>
        /// 把所有边沿触发型输入清空。
        /// PlayerInputCommandSystem 已把本帧输入写入 CommandComponent 后调用，
        /// 避免一次按键在多帧重复触发。
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
