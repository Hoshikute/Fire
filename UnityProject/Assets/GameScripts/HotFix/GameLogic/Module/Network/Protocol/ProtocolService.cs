using UnityEngine;
using System.Net.Sockets;
using System.Collections.Generic;
using System;
using System.Net;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using TEngine;

#if !UNITY_EDITOR && UNITY_WEBGL
// WebSocket 支持需要引入 YLWebSocket 库
// using YLWebSocket;
#endif

namespace GameLogic
{
    /// <summary>
    /// 二进制协议网络服务
    /// 支持 TCP/UDP/WebSocket 协议，使用自定义二进制格式传输消息
    /// </summary>
    public class ProtocolService : INetworkInterface
    {
        #region 常量定义

        public const int TYPE_string = 1;
        public const int TYPE_int32 = 2;
        public const int TYPE_double = 3;
        public const int TYPE_bool = 4;
        public const int TYPE_custom = 5;
        public const int TYPE_int8 = 6;
        public const int TYPE_int16 = 7;
        public const int RT_repeated = 1;
        public const int RT_equired = 0;

        public const string c_ProtocolFileName = "ProtocolInfo";
        public const string c_methodNameInfoFileName = "MethodInfo";

        #endregion

        #region 成员变量

        private Dictionary<string, List<Dictionary<string, object>>> m_protocolInfo;
        private Dictionary<int, string> m_methodNameInfo;
        private Dictionary<string, int> m_methodIndexInfo;

#if !UNITY_EDITOR && UNITY_WEBGL
        // WebSocket 支持需要引入 YLWebSocket 库
        // private WebSocket m_socket;
#else
        private Socket m_Socket;
        private byte[] m_readData;
        private AsyncCallback m_acb = null;
        private Thread m_connThread;
#endif

        #endregion

        #region 初始化

        public override void Init()
        {
            m_protocolInfo = ReadProtocolInfo(ReadTextFile(c_ProtocolFileName));
            ReadMethodNameInfo(
                out m_methodNameInfo,
                out m_methodIndexInfo,
                ReadTextFile(c_methodNameInfoFileName));

#if !UNITY_EDITOR && UNITY_WEBGL
            // WebSocket 初始化
            // GameObject web = new GameObject("WebSocket");
            // WebSocketManager manager = web.AddComponent<WebSocketManager>();
            // m_socket = WebSocketManager.instance.GetSocket(m_IPaddress);
#else
            m_acb = new AsyncCallback(EndReceive);
            m_readData = new byte[102400];
            m_messageBuffer = new byte[204800];
            m_head = 0;
            m_total = 0;
#endif
        }

        /// <summary>
        /// 从 Resources 或配置目录读取文本文件
        /// </summary>
        private string ReadTextFile(string fileName)
        {
            // 尝试从 Resources 加载
            TextAsset asset = Resources.Load<TextAsset>($"Protocol/{fileName}");
            if (asset != null)
            {
                return asset.text;
            }

            // 尝试从 StreamingAssets 加载
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Protocol", $"{fileName}.txt");
            if (System.IO.File.Exists(path))
            {
                return System.IO.File.ReadAllText(path);
            }

            Log.Warning($"[ProtocolService] Protocol file not found: {fileName}");
            return string.Empty;
        }

        public override void GetIPAddress()
        {
            m_IPaddress = "127.0.0.1";
            m_port = 7001;
        }

        public override void SetIPAddress(string IP, int port)
        {
            m_IPaddress = IP;
            m_port = port;

#if !UNITY_EDITOR && UNITY_WEBGL
            // m_IPaddress = "ws://" + m_IPaddress + ":" + port;
#endif
        }

        #endregion

        #region 连接管理

        public override void Close()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            // if (m_socket != null && (m_socket.state == WebSocket.State.Connecting || m_socket.state == WebSocket.State.Connected))
            // {
            //     m_socket.Close();
            // }
#else
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
#endif
        }

        public override void Connect()
        {
            Close();

#if !UNITY_EDITOR && UNITY_WEBGL
            // WebSocket 连接
            // try
            // {
            //     m_socket = WebSocketManager.instance.GetSocket(m_IPaddress);
            //     if (m_socket.state == WebSocket.State.Closed)
            //     {
            //         m_socket.onConnected += OnConnected;
            //         m_socket.onClosed += OnClosed;
            //         m_socket.onReceived += OnReceived;
            //         m_socket.Connect();
            //         m_ConnectStatusCallback(NetworkState.Connecting);
            //     }
            // }
            // catch (Exception e)
            // {
            //     Debug.LogError($"[ProtocolService] WebSocket connect error: {e}");
            // }
#else
            m_connThread = null;
            m_connThread = new Thread(new ThreadStart(RequestConnect));
            m_connThread.Start();
#endif
        }

#if !(!UNITY_EDITOR && UNITY_WEBGL)
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

                // 异步连接 + 5秒超时
                IAsyncResult asyncResult = m_Socket.BeginConnect(ipe, null, null);
                if (!asyncResult.AsyncWaitHandle.WaitOne(5000))
                {
                    m_Socket.Close();
                    m_Socket = null;
                    throw new SocketException((int)SocketError.TimedOut);
                }
                m_Socket.EndConnect(asyncResult);

                isConnect = true;
                StartReceive();

                m_ConnectStatusCallback(NetworkState.Connected);
            }
            catch (Exception e)
            {
                Log.Error($"[ProtocolService] Connect failed IP:{m_IPaddress} Port:{m_port} Error:{e}");
                isConnect = false;
                m_ConnectStatusCallback(NetworkState.FaildToConnect);
            }
        }

        void StartReceive()
        {
            m_Socket.BeginReceive(m_readData, 0, m_readData.Length, SocketFlags.None, m_acb, m_Socket);
        }

        void EndReceive(IAsyncResult iar)
        {
            Socket remote = (Socket)iar.AsyncState;
            int recv = remote.EndReceive(iar);
            if (recv > 0)
            {
                SpiltMessage(m_readData, recv);
            }

            StartReceive();
        }
#else
        // WebSocket 回调
        // public void OnClosed() { ... }
        // public void OnConnected() { ... }
        // public void OnReceived(byte[] data) { ReceiveDataLoad(data); }
#endif

        #endregion

        #region 消息发送

        public void Send(byte[] sendbytes)
        {
#if !(!UNITY_EDITOR && UNITY_WEBGL)
            try
            {
                m_Socket.Send(sendbytes);
            }
            catch (Exception e)
            {
                Log.Error($"[ProtocolService] Send error: {e.Message}");
            }
#else
            // m_socket?.Send(sendbytes);
#endif
        }

        public override void SendMessage(string MessageType, Dictionary<string, object> data)
        {
            ByteArray msg = new ByteArray();
            msg.clear();

            List<byte> message = GetSendByte(MessageType, data);

            int len = 3 + message.Count;
            int method = GetMethodIndex(MessageType);

            msg.WriteShort(len);
            msg.WriteByte((byte)(method / 100));
            msg.WriteShort(method);

            if (message != null)
                msg.bytes.AddRange(message);
            else
                msg.WriteInt(0);

            Send(msg.Buffer);
        }

        #endregion

        #region 缓冲区管理

        private byte[] m_messageBuffer;
        private int m_head = 0;
        private int m_total = 0;

        void SpiltMessage(byte[] bytes, int length)
        {
            WriteBytes(bytes, length);
            int i = 0;

            while (GetBufferLength() != 0 && ReadLength() <= GetBufferLength())
            {
                ReceiveDataLoad(ReadByte(ReadLength()));

                if (i > 100)
                {
                    break;
                }
            }
        }

        void WriteBytes(byte[] bytes, int length)
        {
            for (int i = 0; i < length; i++)
            {
                int pos = m_total + i;

                if (pos >= m_messageBuffer.Length)
                {
                    pos -= m_messageBuffer.Length;
                }

                m_messageBuffer[pos] = bytes[i];
            }

            m_total += length;

            if (m_total >= m_messageBuffer.Length)
            {
                m_total -= m_messageBuffer.Length;
            }
        }

        int ReadLength()
        {
            int result = (int)m_messageBuffer[m_head] << 8;

            int nextPos = m_head + 1;

            if (nextPos >= m_messageBuffer.Length)
            {
                nextPos = 0;
            }

            result += (int)m_messageBuffer[nextPos];
            return result + 2;
        }

        int GetBufferLength()
        {
            if (m_total >= m_head)
            {
                return m_total - m_head;
            }
            else
            {
                return m_total + (m_messageBuffer.Length - m_head);
            }
        }

        byte[] ReadByte(int length)
        {
            byte[] bytes = new byte[length];

            if (m_head + length < m_messageBuffer.Length)
            {
                Array.Copy(m_messageBuffer, m_head, bytes, 0, length);
                m_head += length;
            }
            else
            {
                int cutLength = m_messageBuffer.Length - m_head;

                Array.Copy(m_messageBuffer, m_head, bytes, 0, cutLength);
                Array.Copy(m_messageBuffer, 0, bytes, cutLength, length - cutLength);

                m_head = length - cutLength;
            }
            return bytes;
        }

        #endregion

        #region 消息解析

        private void ReceiveDataLoad(byte[] bytes)
        {
            try
            {
                ByteArray ba = new ByteArray();
                ba.clear();
                ba.Add(bytes);

                NetWorkMessage msg = Analysis(ba);
                m_messageCallBack(msg);
            }
            catch (Exception e)
            {
                Log.Error($"[ProtocolService] ReceiveDataLoad error: {e}");
            }
        }

        NetWorkMessage Analysis(ByteArray bytes)
        {
            NetWorkMessage msg = GetMessageByPool();
            bytes.ReadUShort(); // 消息长度
            bytes.ReadByte();   // 模块名

            int methodIndex = bytes.ReadUShort(); // 方法名

            msg.m_MessageType = m_methodNameInfo[methodIndex];
            int re_len = bytes.Length - 5;
            msg.m_data = AnalysisData(msg.m_MessageType, bytes.ReadBytes(re_len));

            return msg;
        }

        #endregion

        #region 协议信息读取

        public static Dictionary<string, List<Dictionary<string, object>>> ReadProtocolInfo(string content)
        {
            Dictionary<string, List<Dictionary<string, object>>> protocolInfo = new Dictionary<string, List<Dictionary<string, object>>>();

            AnalysisProtocolStatus currentStatus = AnalysisProtocolStatus.None;
            List<Dictionary<string, object>> msgInfo = new List<Dictionary<string, object>>();
            Regex rgx = new Regex(@"^message\s(\w+)");

            string[] lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string currentLine = lines[i];

                if (currentStatus == AnalysisProtocolStatus.None)
                {
                    if (currentLine.Contains("message"))
                    {
                        string msgName = rgx.Match(currentLine).Groups[1].Value;

                        msgInfo = new List<Dictionary<string, object>>();

                        if (protocolInfo.ContainsKey(msgName))
                        {
                            Debug.LogError($"[ProtocolService] Duplicate protocol key: {msgName}");
                        }
                        else
                        {
                            protocolInfo.Add(msgName, msgInfo);
                        }

                        currentStatus = AnalysisProtocolStatus.Message;
                    }
                }
                else
                {
                    if (currentLine.Contains("}"))
                    {
                        currentStatus = AnalysisProtocolStatus.None;
                        msgInfo = null;
                    }
                    else
                    {
                        if (currentLine.Contains("required"))
                        {
                            Dictionary<string, object> currentFieldInfo = new Dictionary<string, object>();
                            currentFieldInfo.Add("spl", RT_equired);
                            AddName(currentLine, currentFieldInfo);
                            AddType(currentLine, currentFieldInfo);
                            msgInfo.Add(currentFieldInfo);
                        }
                        else if (currentLine.Contains("repeated"))
                        {
                            Dictionary<string, object> currentFieldInfo = new Dictionary<string, object>();
                            currentFieldInfo.Add("spl", RT_repeated);
                            AddName(currentLine, currentFieldInfo);
                            AddType(currentLine, currentFieldInfo);
                            msgInfo.Add(currentFieldInfo);
                        }
                    }
                }
            }

            return protocolInfo;
        }

        static Regex m_TypeRgx = new Regex(@"^\s+\w+\s+(\w+)\s+\w+");

        static void AddType(string currentLine, Dictionary<string, object> currentFieldInfo)
        {
            if (currentLine.Contains("int32"))
            {
                currentFieldInfo.Add("type", TYPE_int32);
            }
            else if (currentLine.Contains("int16"))
            {
                currentFieldInfo.Add("type", TYPE_int16);
            }
            else if (currentLine.Contains("int8"))
            {
                currentFieldInfo.Add("type", TYPE_int8);
            }
            else if (currentLine.Contains("string"))
            {
                currentFieldInfo.Add("type", TYPE_string);
            }
            else if (currentLine.Contains("double"))
            {
                currentFieldInfo.Add("type", TYPE_double);
            }
            else if (currentLine.Contains("bool"))
            {
                currentFieldInfo.Add("type", TYPE_bool);
            }
            else
            {
                currentFieldInfo.Add("type", TYPE_custom);
                currentFieldInfo.Add("vp", m_TypeRgx.Match(currentLine).Groups[1].Value);
            }
        }

        static Regex m_NameRgx = new Regex(@"^\s+\w+\s+\w+\s+(\w+)");

        static void AddName(string currentLine, Dictionary<string, object> currentFieldInfo)
        {
            currentFieldInfo.Add("name", m_NameRgx.Match(currentLine).Groups[1].Value);
        }

        enum AnalysisProtocolStatus
        {
            None,
            Message
        }

        #endregion

        #region 消息号映射

        public static void ReadMethodNameInfo(out Dictionary<int, string> methodNameInfo, out Dictionary<string, int> methodIndexInfo, string content)
        {
            methodNameInfo = new Dictionary<int, string>();
            methodIndexInfo = new Dictionary<string, int>();

            Regex rgx = new Regex(@"^(\d+),(\w+)");

            string[] lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(","))
                {
                    var res = rgx.Match(lines[i]);

                    string index = res.Groups[1].Value;
                    string indexName = res.Groups[2].Value;

                    if (!string.IsNullOrEmpty(index) && !string.IsNullOrEmpty(indexName))
                    {
                        methodNameInfo.Add(int.Parse(index), indexName);
                        methodIndexInfo.Add(indexName, int.Parse(index));
                    }
                }
            }
        }

        int GetMethodIndex(string messageType)
        {
            try
            {
                return m_methodIndexInfo[messageType];
            }
            catch
            {
                throw new Exception($"[ProtocolService] GetMethodIndex ERROR! Not found: {messageType}");
            }
        }

        #endregion

        #region 数据解析

        Dictionary<string, object> AnalysisData(string MessageType, byte[] bytes)
        {
            string fieldName = "";
            string customType = "";
            int fieldType = 0;
            int repeatType = 0;

            try
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                ByteArray ba = new ByteArray();
                ba.clear();
                ba.Add(bytes);

                string messageTypeTemp = "m_" + MessageType + "_c";
                if (!m_protocolInfo.ContainsKey(messageTypeTemp))
                {
                    throw new Exception($"[ProtocolService] ProtocolInfo not exist: {messageTypeTemp}");
                }

                List<Dictionary<string, object>> tableInfo = m_protocolInfo[messageTypeTemp];

                for (int i = 0; i < tableInfo.Count; i++)
                {
                    fieldType = (int)tableInfo[i]["type"];
                    repeatType = (int)tableInfo[i]["spl"];
                    fieldName = (string)tableInfo[i]["name"];

                    if (fieldType == TYPE_string)
                    {
                        data[fieldName] = repeatType == RT_repeated ? ReadStringList(ba) : ReadString(ba);
                    }
                    else if (fieldType == TYPE_bool)
                    {
                        data[fieldName] = repeatType == RT_repeated ? ReadBoolList(ba) : ReadBool(ba);
                    }
                    else if (fieldType == TYPE_double)
                    {
                        data[fieldName] = repeatType == RT_repeated ? ReadDoubleList(ba) : ReadDouble(ba);
                    }
                    else if (fieldType == TYPE_int32)
                    {
                        data[fieldName] = repeatType == RT_repeated ? ReadIntList(ba) : ReadInt32(ba);
                    }
                    else if (fieldType == TYPE_int16)
                    {
                        data[fieldName] = repeatType == RT_repeated ? ReadShortList(ba) : ReadInt16(ba);
                    }
                    else if (fieldType == TYPE_int8)
                    {
                        data[fieldName] = repeatType == RT_repeated ? ReadInt8List(ba) : ReadInt8(ba);
                    }
                    else
                    {
                        customType = (string)tableInfo[i]["vp"];
                        data[fieldName] = repeatType == RT_repeated ? ReadDictionaryList(customType, ba) : ReadDictionary(customType, ba);
                    }
                }

                return data;
            }
            catch (Exception e)
            {
                throw new Exception($"[ProtocolService] AnalysisData error: MessageType={MessageType}, Field={fieldName}, Type={GetFieldType(fieldType)}, Repeat={GetRepeatType(repeatType)}, Custom={customType}\n{e}");
            }
        }

        #region 读取方法

        private string ReadString(ByteArray ba)
        {
            uint len = (uint)ba.ReadUShort();
            return ba.ReadUTFBytes(len);
        }

        private List<string> ReadStringList(ByteArray ba)
        {
            List<string> tbl = new List<string>();
            int len1 = ba.ReadUShort();
            ba.ReadUInt();

            for (int i = 0; i < len1; i++)
            {
                tbl.Add(ReadString(ba));
            }
            return tbl;
        }

        private bool ReadBool(ByteArray ba) => ba.ReadBoolean();

        private List<bool> ReadBoolList(ByteArray ba)
        {
            List<bool> tbl = new List<bool>();
            int len1 = ba.ReadUShort();
            ba.ReadUInt();

            for (int i = 0; i < len1; i++)
            {
                tbl.Add(ReadBool(ba));
            }
            return tbl;
        }

        private int ReadInt32(ByteArray ba) => ba.ReadInt32();

        private int ReadInt16(ByteArray ba) => ba.ReadInt16();

        private int ReadInt8(ByteArray ba) => ba.ReadInt8();

        private List<int> ReadIntList(ByteArray ba)
        {
            List<int> tbl = new List<int>();
            int len1 = ba.ReadUShort();
            ba.ReadUInt();

            for (int i = 0; i < len1; i++)
            {
                tbl.Add(ReadInt32(ba));
            }
            return tbl;
        }

        private List<int> ReadShortList(ByteArray ba)
        {
            List<int> tbl = new List<int>();
            int len1 = ba.ReadUShort();
            ba.ReadUInt();

            for (int i = 0; i < len1; i++)
            {
                tbl.Add(ReadInt16(ba));
            }
            return tbl;
        }

        private List<int> ReadInt8List(ByteArray ba)
        {
            List<int> tbl = new List<int>();
            int len1 = ba.ReadUShort();
            ba.ReadUInt();

            for (int i = 0; i < len1; i++)
            {
                tbl.Add(ReadInt8(ba));
            }
            return tbl;
        }

        private double ReadDouble(ByteArray ba)
        {
            double tem_double = ba.ReadDouble();
            return Math.Floor(tem_double * 1000) / 1000;
        }

        private List<double> ReadDoubleList(ByteArray ba)
        {
            List<double> tbl = new List<double>();
            int len1 = ba.ReadUShort();
            ba.ReadUInt();

            for (int i = 0; i < len1; i++)
            {
                tbl.Add(ReadDouble(ba));
            }
            return tbl;
        }

        private Dictionary<string, object> ReadDictionary(string dictName, ByteArray ba)
        {
            int fieldType = 0;
            int repeatType = 0;
            string fieldName = null;
            string customType = null;

            try
            {
                int st_len = ba.ReadUInt();

                Dictionary<string, object> tbl = new Dictionary<string, object>();

                if (st_len == 0)
                {
                    return tbl;
                }

                List<Dictionary<string, object>> tableInfo = m_protocolInfo[dictName];

                for (int i = 0; i < tableInfo.Count; i++)
                {
                    fieldType = (int)tableInfo[i]["type"];
                    repeatType = (int)tableInfo[i]["spl"];
                    fieldName = (string)tableInfo[i]["name"];

                    if (fieldType == TYPE_string)
                        tbl[fieldName] = repeatType == RT_repeated ? ReadStringList(ba) : ReadString(ba);
                    else if (fieldType == TYPE_bool)
                        tbl[fieldName] = repeatType == RT_repeated ? ReadBoolList(ba) : ReadBool(ba);
                    else if (fieldType == TYPE_double)
                        tbl[fieldName] = repeatType == RT_repeated ? ReadDoubleList(ba) : ReadDouble(ba);
                    else if (fieldType == TYPE_int32)
                        tbl[fieldName] = repeatType == RT_repeated ? ReadIntList(ba) : ReadInt32(ba);
                    else if (fieldType == TYPE_int16)
                        tbl[fieldName] = repeatType == RT_repeated ? ReadShortList(ba) : ReadInt16(ba);
                    else if (fieldType == TYPE_int8)
                        tbl[fieldName] = repeatType == RT_repeated ? ReadInt8List(ba) : ReadInt8(ba);
                    else
                    {
                        customType = (string)tableInfo[i]["vp"];
                        tbl[fieldName] = repeatType == RT_repeated ? ReadDictionaryList(customType, ba) : ReadDictionary(customType, ba);
                    }
                }
                return tbl;
            }
            catch (Exception e)
            {
                throw new Exception($"[ProtocolService] ReadDictionary error: DictName={dictName}, Field={fieldName}, Type={GetFieldType(fieldType)}, Repeat={GetRepeatType(repeatType)}, Custom={customType}\n{e}");
            }
        }

        private List<Dictionary<string, object>> ReadDictionaryList(string str, ByteArray ba)
        {
            List<Dictionary<string, object>> stbl = new List<Dictionary<string, object>>();
            int len1 = ba.ReadUShort();
            ba.ReadUInt();

            for (int i = 0; i < len1; i++)
            {
                stbl.Add(ReadDictionary(str, ba));
            }
            return stbl;
        }

        #endregion

        #endregion

        #region 数据序列化

        List<byte> GetSendByte(string messageType, Dictionary<string, object> data)
        {
            try
            {
                string messageTypeTemp = "m_" + messageType + "_s";
                if (!m_protocolInfo.ContainsKey(messageTypeTemp))
                {
                    throw new Exception($"[ProtocolService] ProtocolInfo not exist: {messageTypeTemp}");
                }

                return GetCustomTypeByte(messageTypeTemp, data);
            }
            catch (Exception e)
            {
                throw new Exception($"[ProtocolService] GetSendByte error: messageType={messageType}\n{e}");
            }
        }

        int GetStringListLength(List<object> list)
        {
            int len = 0;
            for (int i = 0; i < list.Count; i++)
            {
                byte[] bs = Encoding.UTF8.GetBytes((string)list[i]);
                len = len + bs.Length;
            }
            return len;
        }

        List<List<byte>> m_arrayCache = new List<List<byte>>();

        int GetCustomListLength(string customType, List<object> list)
        {
            m_arrayCache.Clear();
            int len = 0;
            for (int i = 0; i < list.Count; i++)
            {
                List<byte> bs = GetCustomTypeByte(customType, (Dictionary<string, object>)list[i]);
                m_arrayCache.Add(bs);
                len = len + bs.Count + 4;
            }
            return len;
        }

        private List<byte> GetCustomTypeByte(string customType, Dictionary<string, object> data)
        {
            string fieldName = null;
            int fieldType = 0;
            int repeatType = 0;

            try
            {
                ByteArray Bytes = new ByteArray();
                Bytes.clear();

                if (!m_protocolInfo.ContainsKey(customType))
                {
                    throw new Exception($"[ProtocolService] ProtocolInfo not exist: {customType}");
                }

                List<Dictionary<string, object>> tableInfo = m_protocolInfo[customType];

                for (int i = 0; i < tableInfo.Count; i++)
                {
                    Dictionary<string, object> currentField = tableInfo[i];
                    fieldType = (int)currentField["type"];
                    fieldName = (string)currentField["name"];
                    repeatType = (int)currentField["spl"];

                    if (fieldType == TYPE_string)
                    {
                        if (data.ContainsKey(fieldName))
                        {
                            if (repeatType == RT_equired)
                            {
                                Bytes.WriteString((string)data[fieldName]);
                            }
                            else
                            {
                                List<object> list = (List<object>)data[fieldName];
                                Bytes.WriteShort(list.Count);
                                Bytes.WriteInt(GetStringListLength(list));
                                for (int i2 = 0; i2 < list.Count; i2++)
                                {
                                    Bytes.WriteString((string)list[i2]);
                                }
                            }
                        }
                        else
                        {
                            Bytes.WriteShort(0);
                        }
                    }
                    else if (fieldType == TYPE_bool)
                    {
                        if (data.ContainsKey(fieldName))
                        {
                            if (repeatType == RT_equired)
                            {
                                Bytes.WriteBoolean((bool)data[fieldName]);
                            }
                            else
                            {
                                List<object> tb = (List<object>)data[fieldName];
                                Bytes.WriteShort(tb.Count);
                                Bytes.WriteInt(tb.Count);
                                for (int i2 = 0; i2 < tb.Count; i2++)
                                {
                                    Bytes.WriteBoolean((bool)tb[i2]);
                                }
                            }
                        }
                    }
                    else if (fieldType == TYPE_double)
                    {
                        if (data.ContainsKey(fieldName))
                        {
                            if (repeatType == RT_equired)
                            {
                                Bytes.WriteDouble((float)data[fieldName]);
                            }
                            else
                            {
                                List<object> tb = (List<object>)data[fieldName];
                                Bytes.WriteShort(tb.Count);
                                Bytes.WriteInt(tb.Count * 8);
                                for (int j = 0; j < tb.Count; j++)
                                {
                                    Bytes.WriteDouble((float)tb[j]);
                                }
                            }
                        }
                    }
                    else if (fieldType == TYPE_int32)
                    {
                        if (data.ContainsKey(fieldName))
                        {
                            if (repeatType == RT_equired)
                            {
                                Bytes.WriteInt(int.Parse(data[fieldName].ToString()));
                            }
                            else
                            {
                                List<object> tb = (List<object>)data[fieldName];
                                Bytes.WriteShort(tb.Count);
                                Bytes.WriteInt(tb.Count * 4);
                                for (int i2 = 0; i2 < tb.Count; i2++)
                                {
                                    Bytes.WriteInt(int.Parse(tb[i2].ToString()));
                                }
                            }
                        }
                    }
                    else if (fieldType == TYPE_int16)
                    {
                        if (data.ContainsKey(fieldName))
                        {
                            if (repeatType == RT_equired)
                            {
                                Bytes.WriteShort(int.Parse(data[fieldName].ToString()));
                            }
                            else
                            {
                                List<object> tb = (List<object>)data[fieldName];
                                Bytes.WriteShort(tb.Count);
                                Bytes.WriteInt(tb.Count * 2);
                                for (int i2 = 0; i2 < tb.Count; i2++)
                                {
                                    Bytes.WriteShort(int.Parse(tb[i2].ToString()));
                                }
                            }
                        }
                    }
                    else if (fieldType == TYPE_int8)
                    {
                        if (data.ContainsKey(fieldName))
                        {
                            if (repeatType == RT_equired)
                            {
                                Bytes.WriteInt8(int.Parse(data[fieldName].ToString()));
                            }
                            else
                            {
                                List<object> tb = (List<object>)data[fieldName];
                                Bytes.WriteShort(tb.Count);
                                Bytes.WriteInt(tb.Count);
                                for (int i2 = 0; i2 < tb.Count; i2++)
                                {
                                    Bytes.WriteInt8(int.Parse(tb[i2].ToString()));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (data.ContainsKey(fieldName))
                        {
                            customType = (string)currentField["vp"];
                            if (repeatType == RT_equired)
                            {
                                List<byte> byteTmp = GetCustomTypeByte(customType, (Dictionary<string, object>)data[fieldName]);
                                Bytes.WriteInt(byteTmp.Count);
                                Bytes.bytes.AddRange(byteTmp);
                            }
                            else
                            {
                                List<object> tb = (List<object>)data[fieldName];
                                Bytes.WriteShort(tb.Count);
                                Bytes.WriteInt(GetCustomListLength(customType, tb));

                                for (int j = 0; j < m_arrayCache.Count; j++)
                                {
                                    List<byte> tempb = m_arrayCache[j];
                                    Bytes.WriteInt(tempb.Count);
                                    Bytes.bytes.AddRange(tempb);
                                }
                            }
                        }
                    }
                }
                return Bytes.bytes;
            }
            catch (Exception e)
            {
                throw new Exception($"[ProtocolService] GetCustomTypeByte error: CustomType={customType}, Field={fieldName}, Type={GetFieldType(fieldType)}, Repeat={GetRepeatType(repeatType)}\n{e}");
            }
        }

        #endregion

        #region 辅助方法

        string GetFieldType(int fieldType)
        {
            switch (fieldType)
            {
                case TYPE_string: return "TYPE_string";
                case TYPE_int32: return "TYPE_int32";
                case TYPE_int16: return "TYPE_int16";
                case TYPE_int8: return "TYPE_int8";
                case TYPE_double: return "TYPE_double";
                case TYPE_bool: return "TYPE_bool";
                case TYPE_custom: return "TYPE_custom";
                default: return "Error";
            }
        }

        string GetRepeatType(int repeatType)
        {
            switch (repeatType)
            {
                case RT_repeated: return "RT_repeated";
                case RT_equired: return "RT_equired";
                default: return "Error";
            }
        }

        #endregion
    }
}
