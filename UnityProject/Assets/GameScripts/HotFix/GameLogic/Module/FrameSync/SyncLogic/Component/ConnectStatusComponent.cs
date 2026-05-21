using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 连接状态组件
    /// </summary>
    public class ConnectStatusComponent : SingletonComponent
    {
        public int rtt = 0;
        public List<int> unConfirmFrame = new List<int>();
        public List<string> confirmCommand = new List<string>();
        public int aheadFrame = -1;
        public int ClearFrame = -1;
        public int isAllFrame = -1;
    }
}
