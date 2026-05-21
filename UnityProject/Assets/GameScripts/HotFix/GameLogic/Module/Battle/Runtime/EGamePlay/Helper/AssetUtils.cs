using Cysharp.Threading.Tasks;
using EGamePlay.Combat;
using System.IO;
using GameLogic.Battle.Runtime.Compat;
using TEngine;

namespace GameUtils
{
    public static class AssetUtils
    {
#if UNITY
        // GameBattle 程序集无法访问 GameModule，使用 ModuleSystem
        private static IResourceModule ResourceModule => ModuleSystem.GetModule<IResourceModule>();

        /// <summary>
        /// 同步加载资源对象。
        /// 注意：仅用于已预加载的小资源，推荐使用异步版本。
        /// </summary>
        public static T LoadObject<T>(string location) where T : UnityEngine.Object
        {
            return ResourceModule.LoadAsset<T>(location);
        }

        /// <summary>
        /// 异步加载资源对象（推荐）。
        /// </summary>
        public static async UniTask<T> LoadObjectAsync<T>(string location) where T : UnityEngine.Object
        {
            return await ResourceModule.LoadAssetAsync<T>(location);
        }
#else
        public static T LoadObject<T>(string path)
        {
            var text = File.ReadAllText($"../../{path}.json");
            var obj = JsonHelper.FromJson<T>(text);
            return obj;
        }
#endif
    }
}
