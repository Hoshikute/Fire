namespace GameLogic
{
    /// <summary>
    /// 帧同步时间配置。
    /// 集中管理逻辑帧间隔，避免 FrameSyncModule、PlayerStateSystem、PlayerViewSystem 各自硬编码。
    ///
    /// 使用 const 确保零开销内联；如需持久化配置文件支持，后续可扩展为 static 属性从 JSON 读取。
    /// </summary>
    public static class FrameConfig
    {
        /// <summary>逻辑帧间隔（毫秒）。默认 200ms，即每秒 5 帧。</summary>
        public const int LogicFrameIntervalMs = 200;

        /// <summary>逻辑帧间隔（秒），供表现层插值计算使用。</summary>
        public const float LogicFrameDurationSeconds = LogicFrameIntervalMs / 1000f;

        /// <summary>全局逻辑帧计数器。由 FrameSyncModule 在每逻辑帧递增。</summary>
        public static long FrameId { get; internal set; }
    }
}
