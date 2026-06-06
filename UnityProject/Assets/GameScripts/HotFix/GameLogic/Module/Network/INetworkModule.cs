using System;
using System.Net.Sockets;

namespace GameLogic
{
    /// <summary>
    /// 网络模块接口
    /// </summary>
    public interface INetworkModule
    {
        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 网络模块是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 初始化底层网络实现
        /// </summary>
        void Init<T>(ProtocolType protocolType = ProtocolType.Tcp) where T : INetworkInterface, new();

        /// <summary>
        /// 网络状态变化事件
        /// </summary>
        event Action<NetworkState> StatusChanged;

        /// <summary>
        /// 网络消息事件
        /// </summary>
        event Action<NetWorkMessage> MessageReceived;

        /// <summary>
        /// 连接服务器
        /// </summary>
        void Connect(string host, int port);

        /// <summary>
        /// 断开连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 发送消息
        /// </summary>
        void SendMessage(string messageType, System.Collections.Generic.Dictionary<string, object> data);

        /// <summary>
        /// 发送消息（自动提取MT字段）
        /// </summary>
        void SendMessage(System.Collections.Generic.Dictionary<string, object> data);

        /// <summary>
        /// 设置服务器地址
        /// </summary>
        void SetServer(string ip, int port);
    }
}
