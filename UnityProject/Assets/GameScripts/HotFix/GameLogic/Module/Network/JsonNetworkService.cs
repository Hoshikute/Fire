using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameLogic
{
    /// <summary>
    /// JSON 序列化网络服务
    /// 支持 TCP/UDP 协议，使用 JSON 格式传输消息
    /// </summary>
    public class JsonNetworkService : INetworkInterface
    {
        /// <summary>
        /// 消息结尾符
        /// </summary>
        public const char c_endChar = '&';

        /// <summary>
        /// 文本中如果有结尾符则替换成这个
        /// </summary>
        public const string c_endCharReplaceString = "<FCP:AND>";

        private Socket m_Socket;
        private byte[] m_readData = new byte[1024];
        private Thread m_connThread;

        public override void Init()
        {
        }

        public override void GetIPAddress()
        {
            // 可从配置读取默认服务器地址
            // m_IPaddress = "127.0.0.1";
            // m_port = 7001;
        }

        public override void SetIPAddress(string IP, int port)
        {
            m_IPaddress = IP;
            m_port = port;
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public override void Connect()
        {
            Close();

            m_connThread = null;
            m_connThread = new Thread(new ThreadStart(RequestConnect));
            m_connThread.Start();
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public override void Close()
        {
            isConnect = false;
            if (m_Socket != null)
            {
                m_Socket.Close(0);
                m_Socket = null;
            }
            if (m_connThread != null)
            {
                m_connThread.Join();
                m_connThread.Abort();
            }
            m_connThread = null;
        }

        /// <summary>
        /// 请求连接线程
        /// </summary>
        void RequestConnect()
        {
            try
            {
                m_ConnectStatusCallback(NetworkState.Connecting);

                SocketType socketType = SocketType.Stream;

                if (m_protocolType == ProtocolType.Udp)
                {
                    socketType = SocketType.Dgram;
                }

                m_Socket = new Socket(AddressFamily.InterNetwork, socketType, m_protocolType);
                IPAddress ip = IPAddress.Parse(m_IPaddress);
                IPEndPoint ipe = new IPEndPoint(ip, m_port);
                m_Socket.Connect(ipe);
                isConnect = true;
                StartReceive();

                m_ConnectStatusCallback(NetworkState.Connected);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonNetworkService] Connect failed: {e}");
                isConnect = false;
                m_ConnectStatusCallback(NetworkState.FaildToConnect);
            }
        }

        void StartReceive()
        {
            m_Socket.BeginReceive(m_readData, 0, m_readData.Length, SocketFlags.None, new AsyncCallback(EndReceive), m_Socket);
        }

        void EndReceive(IAsyncResult iar)
        {
            Socket remote = (Socket)iar.AsyncState;
            int recv = remote.EndReceive(iar);
            if (recv > 0)
            {
                DealMessage(Encoding.UTF8.GetString(m_readData, 0, recv));
            }

            StartReceive();
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public override void SendMessage(string MessageType, Dictionary<string, object> data)
        {
            try
            {
                if (!data.ContainsKey("MT"))
                {
                    data.Add("MT", MessageType);
                }

                string mes = JsonConvert.SerializeObject(data);
                mes = mes.Replace(c_endChar.ToString(), c_endCharReplaceString);
                byte[] bytes = Encoding.UTF8.GetBytes(mes + c_endChar);

                m_Socket.Send(bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonNetworkService] SendMessage error: {e}");
            }
        }

        StringBuilder m_buffer = new StringBuilder();

        /// <summary>
        /// 处理接收到的消息（处理粘包）
        /// </summary>
        public void DealMessage(string s)
        {
            bool isEnd = false;

            if (s.Substring(s.Length - 1, 1) == c_endChar.ToString())
            {
                isEnd = true;
            }

            m_buffer.Append(s);

            string buffer = m_buffer.ToString();

            m_buffer.Remove(0, m_buffer.Length);

            string[] str = buffer.Split(c_endChar);

            for (int i = 0; i < str.Length; i++)
            {
                if (i != str.Length - 1)
                {
                    CallBack(str[i]);
                }
                else
                {
                    if (isEnd)
                    {
                        CallBack(str[i]);
                    }
                    else
                    {
                        m_buffer.Append(str[i]);
                    }
                }
            }
        }

        public void CallBack(string s)
        {
            try
            {
                if (s != null && s != "")
                {
                    NetWorkMessage msg = GetMessageByPool();

                    s = Uri.UnescapeDataString(s);
                    s = s.Replace(c_endCharReplaceString, c_endChar.ToString());
                    Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(s);

                    msg.m_data = data;
                    msg.m_MessageType = data["MT"].ToString();

                    m_messageCallBack(msg);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonNetworkService] Message error ->{s}<-\n{e}");
            }
        }
    }
}
