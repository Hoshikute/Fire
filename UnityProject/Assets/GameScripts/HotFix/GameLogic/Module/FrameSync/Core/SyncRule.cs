namespace GameLogic
{
    /// <summary>
    /// 同步规则
    /// </summary>
    public enum SyncRule
    {
        /// <summary>
        /// 状态同步，所有对实体的操作都交给服务器下发
        /// </summary>
        Status,

        /// <summary>
        /// 帧同步，本地计算所有结果
        /// </summary>
        Frame,
    }
}
