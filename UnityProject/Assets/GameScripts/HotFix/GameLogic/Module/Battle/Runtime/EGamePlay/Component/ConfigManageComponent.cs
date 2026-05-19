using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ET;
using System;
using System.Linq;

#if !EGAMEPLAY_ET
namespace EGamePlay.Combat
{
    public static class ConfigHelper
    {
        public static T Get<T>(int id) where T : class, IConfig
        {
            return ConfigManageComponent.Instance.Get<T>(id);
        }

        public static Dictionary<int, T> GetAll<T>() where T : class, IConfig
        {
            return ConfigManageComponent.Instance.GetAll<T>();
        }
    }

    public class ConfigManageComponent : Component
    {
        public static ConfigManageComponent    Instance            { get; private set; }
        public        Dictionary<Type, object> TypeConfigCategarys { get; set; } = new Dictionary<Type, object>();


        public override void Awake(object initData)
        {
            Instance = this;
            var configsCollector = initData as ReferenceCollector;
            if (configsCollector == null)
            {
                return;
            }

            foreach (var item in configsCollector.data)
            {
                var configTypeName     = $"ET.{item.gameObject.name}";
                var configType         = FindType(configTypeName);
                var typeName           = $"ET.{item.gameObject.name}Category";
                var configCategoryType = FindType(typeName);
                if (configType == null || configCategoryType == null)
                {
                    throw new InvalidOperationException($"Battle config type not found. Config='{configTypeName}', Category='{typeName}'.");
                }

                var configCategory     = Activator.CreateInstance(configCategoryType) as ACategory;
                configCategory.ConfigText = (item.gameObject as TextAsset).text;
                configCategory.BeginInit();
                TypeConfigCategarys.Add(configType, configCategory);
            }
        }

        public T Get<T>(int id) where T : class, IConfig
        {
            var category = TypeConfigCategarys[typeof(T)] as ACategory<T>;
            return category.Get(id);
        }

        public Dictionary<int, T> GetAll<T>() where T : class, IConfig
        {
            var category = TypeConfigCategarys[typeof(T)] as ACategory<T>;
            return category.GetAll();
        }

        private static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(type => type != null);
        }
    }
}
#endif
