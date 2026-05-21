using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 服务器缓存组件
    /// </summary>
    public class ServerCacheComponent : SingletonComponent
    {
        public List<ServiceMessageInfo> m_messageList = new List<ServiceMessageInfo>();
    }
}
