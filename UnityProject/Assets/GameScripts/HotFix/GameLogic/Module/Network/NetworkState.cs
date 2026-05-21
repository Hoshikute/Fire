namespace GameLogic
{
    /// <summary>
    /// 网络连接状态
    /// </summary>
    public enum NetworkState
    {
        Connected,
        Connecting,
        ConnectBreak,
        FaildToConnect,
    }

    /// <summary>
    /// 网络消息
    /// </summary>
    public class NetWorkMessage
    {
        public string m_MessageType;
        public System.Collections.Generic.Dictionary<string, object> m_data;
    }

    /// <summary>
    /// 消息回调委托
    /// </summary>
    public delegate void MessageCallBack(NetWorkMessage message);

    /// <summary>
    /// 连接状态回调委托
    /// </summary>
    public delegate void ConnectStatusCallBack(NetworkState connectStatus);
}
