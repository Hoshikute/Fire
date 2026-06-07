using Animancer;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// 动画提供者接口 — 由各状态数据类实现，暴露所有持有的 ITransition
    /// 用于 Player 初始化时统一预热动画 clip 数据。
    /// </summary>
    public interface IAnimationProvider
    {
        void CollectTransitions(List<ITransition> results);
    }
}
