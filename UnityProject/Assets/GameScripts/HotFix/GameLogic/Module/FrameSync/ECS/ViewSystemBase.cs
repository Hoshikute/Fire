namespace GameLogic
{
    /// <summary>
    /// 客户端视图系统基类。
    /// 只允许在渲染帧生命周期（BeforeUpdate/Update/LateUpdate）里做表现层工作，
    /// 不能参与逻辑帧或回滚重算，避免 Transform/Animancer 等 UnityEngine 副作用污染确定性。
    /// </summary>
    public abstract class ViewSystemBase : SystemBase
    {
        public sealed override void OnlyCallByRecalc(int frame, int deltaTime) { }
        public sealed override void NoRecalcBeforeFixedUpdate(int deltaTime) { }
        public sealed override void BeforeFixedUpdate(int deltaTime) { }
        public sealed override void FixedUpdate(int deltaTime) { }
        public sealed override void LateFixedUpdate(int deltaTime) { }
        public sealed override void NoRecalcLateFixedUpdate(int deltaTime) { }
        public sealed override void EndFrame(int deltaTime) { }
    }
}
