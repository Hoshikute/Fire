using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// Game 世界控制器接口。
    /// 负责管理 Game 场景的初始化顺序和生命周期。
    /// </summary>
    public interface IBattleContext
    {
        /// <summary>
        /// 场景是否已初始化完成。
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 初始化 Game 场景。
        /// </summary>
        UniTask InitializeGameScene();
    }
}
