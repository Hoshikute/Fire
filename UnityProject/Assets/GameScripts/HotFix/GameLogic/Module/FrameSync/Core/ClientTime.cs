using System;

namespace GameLogic
{
    /// <summary>
    /// 客户端时间工具
    /// </summary>
    public static class ClientTime
    {
        public const int Tick2ms = 10000;

        /// <summary>
        /// 获取当前时间（毫秒）
        /// </summary>
        public static int GetTime()
        {
            return (int)(DateTime.Now.Ticks / Tick2ms);
        }
    }
}
