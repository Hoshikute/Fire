using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic.Common.Resource
{
    /// <summary>
    /// 资源加载适配器 - 适配 TEngine 资源系统
    /// </summary>
    public static class ResourceManager
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        public static T Load<T>(string path) where T : Object
        {
            return GameModule.Resource.LoadAsset<T>(path);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public static void LoadAsync<T>(string path, System.Action<AsyncOperation, T> callback) where T : Object
        {
            LoadAssetInternal<T>(path, callback).Forget();
        }

        private static async UniTaskVoid LoadAssetInternal<T>(string path, System.Action<AsyncOperation, T> callback) where T : Object
        {
            var asset = await GameModule.Resource.LoadAssetAsync<T>(path);
            callback?.Invoke(null, asset);
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        public static void UnloadAsset(Object asset)
        {
            GameModule.Resource.UnloadAsset(asset);
        }
    }
}
