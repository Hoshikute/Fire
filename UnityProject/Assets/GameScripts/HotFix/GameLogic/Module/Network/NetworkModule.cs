using System;
using System.Collections.Generic;
using System.Net.Sockets;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 网络模块实现
    /// </summary>
    public class NetworkModule : Module, INetworkModule, IUpdateModule
    {
        private INetworkInterface m_network;
        private bool m_isConnect;

        public bool IsConnected => m_isConnect;
        public bool IsInitialized => m_network != null;
        public event Action<NetworkState> StatusChanged;
        public event Action<NetWorkMessage> MessageReceived;

        private List<NetworkState> m_statusList = new List<NetworkState>();
        private List<NetWorkMessage> m_messageList = new List<NetWorkMessage>();

        public override void OnInit()
        {
        }

        public override void Shutdown()
        {
            m_network?.Close();
            m_network = null;
            m_isConnect = false;
            m_messageList.Clear();
            m_statusList.Clear();
        }

        public void Init<T>(ProtocolType protocolType = ProtocolType.Tcp) where T : INetworkInterface, new()
        {
            m_network?.Close();
            m_network = new T();
            m_network.m_protocolType = protocolType;
            m_network.Init();
            m_network.m_messageCallBack = OnReceiveMessage;
            m_network.m_ConnectStatusCallback = OnConnectStatusChange;
        }

        public void SetServer(string ip, int port)
        {
            m_network?.SetIPAddress(ip, port);
        }

        public void Connect(string host, int port)
        {
            SetServer(host, port);
            m_network?.Connect();
        }

        public void Disconnect()
        {
            Log.Info("断开连接");
            m_network?.Close();
            m_isConnect = false;
        }

        public void SendMessage(string messageType, Dictionary<string, object> data)
        {
            if (m_isConnect)
            {
                m_network.SendMessage(messageType, data);
            }
            else
            {
                Log.Error("socket 未连接！");
            }
        }

        public void SendMessage(Dictionary<string, object> data)
        {
            try
            {
                if (m_isConnect)
                {
                    if (!data.ContainsKey("MT"))
                    {
                        Log.Error("NetworkManager SendMessage Error ：消息没有加 MT 字段！");
                        return;
                    }
                    m_network.SendMessage(data["MT"].ToString(), data);
                }
                else
                {
                    Log.Error("socket 未连接！");
                }
            }
            catch (Exception e)
            {
                Log.Error("SendMessage Error " + e.ToString());
            }
        }

        private void OnReceiveMessage(NetWorkMessage message)
        {
            m_messageList.Add(message);
        }

        private void OnConnectStatusChange(NetworkState status)
        {
            m_statusList.Add(status);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            // 主线程处理消息
            if (m_messageList.Count > 0)
            {
                for (int i = 0; i < m_messageList.Count; i++)
                {
                    DispatchMessage(m_messageList[i]);
                }
                m_messageList.Clear();
            }

            // 主线程处理状态变化
            if (m_statusList.Count > 0)
            {
                for (int i = 0; i < m_statusList.Count; i++)
                {
                    DispatchStatus(m_statusList[i]);
                }
                m_statusList.Clear();
            }
        }

        private void DispatchMessage(NetWorkMessage msg)
        {
            try
            {
                MessageReceived?.Invoke(msg);
                Log.Debug($"收到消息: {msg.m_MessageType}");
            }
            catch (Exception e)
            {
                Log.Error($"Message Error: {msg.m_MessageType} - {e}");
            }
        }

        private void DispatchStatus(NetworkState status)
        {
            if (status == NetworkState.Connected)
            {
                m_isConnect = true;
            }
            else
            {
                m_isConnect = false;
            }

            StatusChanged?.Invoke(status);
            Log.Info($"网络状态变化: {status}");
        }
    }
}
