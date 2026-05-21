using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 游戏数据缓存组件
    /// </summary>
    public class GameDataCacheComponent : SingletonComponent
    {
        public List<CommandMsg> m_noExecuteCommandList = new List<CommandMsg>();
    }
}
