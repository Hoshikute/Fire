using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 相机模块接口。
    /// 负责管理游戏相机、UI相机的生命周期和渲染顺序。
    /// </summary>
    public interface ICameraModule
    {
        /// <summary>
        /// 获取当前主相机。
        /// </summary>
        Camera MainCamera { get; }

        /// <summary>
        /// 设置游戏主相机。
        /// </summary>
        /// <param name="camera">游戏相机。</param>
        void SetMainCamera(Camera camera);

        /// <summary>
        /// 绑定 Cinemachine 虚拟相机到 Player。
        /// </summary>
        /// <param name="playerTransform">Player 的 Transform。</param>
        void BindCinemachineToPlayer(Transform playerTransform);

        /// <summary>
        /// 清理场景中多余的相机（场景切换前调用）。
        /// </summary>
        void CleanupExtraCameras();
    }
}
