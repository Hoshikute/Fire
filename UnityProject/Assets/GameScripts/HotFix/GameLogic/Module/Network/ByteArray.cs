using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic
{
    /// <summary>
    /// 二进制数据读写工具类
    /// 支持多种基础类型的序列化与反序列化
    /// </summary>
    public class ByteArray
    {
        public List<byte> bytes = new List<byte>();

        /// <summary>
        /// 添加字节数组到缓冲区
        /// </summary>
        public void Add(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                bytes.Add(buffer[i]);
            }
        }

        /// <summary>
        /// 获取缓冲区长度
        /// </summary>
        public int Length
        {
            get { return bytes.Count; }
        }

        /// <summary>
        /// 清空缓冲区并重置位置
        /// </summary>
        public void clear()
        {
            Postion = 0;
            bytes.Clear();
        }

        /// <summary>
        /// 当前读写位置
        /// </summary>
        public int Postion { get; set; }

        /// <summary>
        /// 获取缓冲区数据副本
        /// </summary>
        public byte[] Buffer
        {
            get { return bytes.ToArray(); }
        }

        #region 读取方法

        public bool ReadBoolean()
        {
            byte b = bytes[Postion];
            Postion += 1;
            return b != 0;
        }

        public byte ReadByte()
        {
            byte result = bytes[Postion];
            Postion += 1;
            return result;
        }

        public byte[] ReadBytes(int len)
        {
            byte[] result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = bytes[i + Postion];
            }
            Postion += len;
            return result;
        }

        public int ReadUInt()
        {
            int result = bytes[3 + Postion] | (bytes[2 + Postion] << 8) | (bytes[1 + Postion] << 16) | (bytes[0 + Postion] << 24);
            Postion += 4;
            return result;
        }

        public int ReadUShort()
        {
            int result = bytes[1 + Postion] | bytes[Postion] << 8;
            Postion += 2;
            return result;
        }

        private byte[] int32Cache = new byte[4];
        public int ReadInt32()
        {
            int32Cache[3] = bytes[Postion];
            int32Cache[2] = bytes[Postion + 1];
            int32Cache[1] = bytes[Postion + 2];
            int32Cache[0] = bytes[Postion + 3];

            int result = BitConverter.ToInt32(int32Cache, 0);
            Postion += 4;
            return result;
        }

        private byte[] int16Cache = new byte[2];
        public int ReadInt16()
        {
            int16Cache[1] = bytes[Postion];
            int16Cache[0] = bytes[Postion + 1];

            int result = BitConverter.ToInt16(int16Cache, 0);
            Postion += 2;
            return result;
        }

        public int ReadInt8()
        {
            int result = bytes[Postion];
            Postion += 1;
            return result;
        }

        private byte[] doubleCache = new byte[8];
        public double ReadDouble()
        {
            for (int i = 0; i < 8; i++)
            {
                doubleCache[7 - i] = bytes[i + Postion];
            }
            Postion += 8;
            return BitConverter.ToDouble(doubleCache, 0);
        }

        public string ReadUTFBytes(uint length)
        {
            if (length == 0)
                return string.Empty;

            byte[] b = new byte[length];
            for (int i = 0; i < length; i++)
            {
                b[i] = bytes[i + Postion];
            }
            Postion += (int)length;

            string decodedString = Encoding.UTF8.GetString(b);
            return decodedString;
        }

        #endregion

        #region 写入方法

        public void WriteInt(int value)
        {
            bytes.Add((byte)(value >> 24));
            bytes.Add((byte)(value >> 16));
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)(value));
        }

        public void WriteShort(int value)
        {
            short tmp = (short)value;

            bytes.Add((byte)(tmp >> 8));
            bytes.Add((byte)(tmp));
        }

        public void WriteInt8(int value)
        {
            bytes.Add((byte)(value));
        }

        public void WriteALLBytes(byte[] bs)
        {
            bytes.AddRange(bs);
        }

        public void WriteBoolean(bool value)
        {
            bytes.Add(value ? ((byte)1) : ((byte)0));
        }

        public void WriteByte(byte value)
        {
            bytes.Add(value);
        }

        public void WriteDouble(double v)
        {
            byte[] temp = BitConverter.GetBytes(v);

            for (int i = 0; i < 8; i++)
            {
                doubleCache[7 - i] = temp[i];
            }

            bytes.AddRange(doubleCache);
        }

        public void WriteString(string content)
        {
            if (content == null)
            {
                content = "";
            }

            byte[] bs = Encoding.UTF8.GetBytes(content);
            WriteShort(bs.Length);
            WriteALLBytes(bs);
        }

        #endregion
    }
}
