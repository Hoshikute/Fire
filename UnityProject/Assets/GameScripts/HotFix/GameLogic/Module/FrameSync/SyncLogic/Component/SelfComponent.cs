namespace GameLogic
{
    /// <summary>
    /// 服务端快照下发的本端玩家标记。
    /// 客户端收到后再派生 PlayerComponent.isLocal，避免网络模式本地预创建权威实体。
    /// </summary>
    public class SelfComponent : ComponentBase
    {
    }
}
