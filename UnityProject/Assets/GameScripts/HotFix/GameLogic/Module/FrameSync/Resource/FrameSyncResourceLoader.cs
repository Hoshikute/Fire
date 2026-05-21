using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic.FrameSync
{
    /// <summary>
    /// 帧同步资源加载器
    /// 统一管理从 LockStepDemo 迁移的资源
    /// </summary>
    public static class FrameSyncResourceLoader
    {
        /// <summary>
        /// 加载角色预制体
        /// </summary>
        /// <param name="characterName">角色名称，如 "hero_01", "male_01", "famale_01"</param>
        /// <param name="parent">父节点</param>
        public static async UniTask<GameObject> LoadCharacterAsync(string characterName, Transform parent = null)
        {
            return await GameModule.Resource.LoadGameObjectAsync(characterName, parent);
        }

        /// <summary>
        /// 加载特效预制体
        /// </summary>
        /// <param name="effectName">特效名称，如 "EFX_fire_001"</param>
        /// <param name="parent">父节点</param>
        public static async UniTask<GameObject> LoadEffectAsync(string effectName, Transform parent = null)
        {
            return await GameModule.Resource.LoadGameObjectAsync(effectName, parent);
        }

        /// <summary>
        /// 加载 Buff 特效
        /// </summary>
        /// <param name="buffFxName">Buff特效名称，如 "jiansu_buff"</param>
        /// <param name="parent">父节点</param>
        public static async UniTask<GameObject> LoadBuffEffectAsync(string buffFxName, Transform parent = null)
        {
            return await GameModule.Resource.LoadGameObjectAsync(buffFxName, parent);
        }

        /// <summary>
        /// 加载武器预制体
        /// </summary>
        /// <param name="weaponName">武器名称</param>
        /// <param name="parent">父节点</param>
        public static async UniTask<GameObject> LoadWeaponAsync(string weaponName, Transform parent = null)
        {
            return await GameModule.Resource.LoadGameObjectAsync(weaponName, parent);
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="configName">配置名称（不含扩展名），如 "SkillData", "BuffData"</param>
        public static async UniTask<TextAsset> LoadConfigAsync(string configName)
        {
            return await GameModule.Resource.LoadAssetAsync<TextAsset>(configName);
        }

        /// <summary>
        /// 卸载配置资源
        /// </summary>
        public static void UnloadConfig(TextAsset config)
        {
            if (config != null)
            {
                GameModule.Resource.UnloadAsset(config);
            }
        }
    }
}
