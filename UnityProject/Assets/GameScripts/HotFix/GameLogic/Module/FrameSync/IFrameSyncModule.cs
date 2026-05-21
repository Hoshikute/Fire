namespace GameLogic
{
    /// <summary>
    /// 帧同步模块接口
    /// </summary>
    public interface IFrameSyncModule
    {
        /// <summary>
        /// 帧间隔时间（毫秒）
        /// </summary>
        int IntervalTime { get; set; }

        /// <summary>
        /// 创建同步世界
        /// </summary>
        WorldBase CreateWorld<T>() where T : WorldBase, new();

        /// <summary>
        /// 销毁同步世界
        /// </summary>
        void DestroyWorld(WorldBase world);
    }
}
