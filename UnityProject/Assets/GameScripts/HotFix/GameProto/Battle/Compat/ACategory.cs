using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace ET
{
    /// <summary>
    /// Battle legacy config compatibility category base.
    /// 这是过渡兼容层，只承载旧版 Battle 配置文本反序列化与索引能力。
    /// </summary>
    public abstract class ACategory : ISupportInitialize
    {
        public abstract Type ConfigType { get; }
        public string ConfigText { get; set; }

        public virtual void BeginInit()
        {
        }

        public virtual void EndInit()
        {
        }
    }

    public abstract class ACategory<T> : ACategory where T : class, IConfig
    {
        protected readonly Dictionary<int, T> dict = new Dictionary<int, T>();
        protected Dictionary<string, T> nameDict;

        public override Type ConfigType => typeof(T);

        public override void BeginInit()
        {
            if (string.IsNullOrWhiteSpace(ConfigText))
            {
                dict.Clear();
                nameDict = null;
                return;
            }

            Dictionary<string, T> data = JsonConvert.DeserializeObject<Dictionary<string, T>>(ConfigText);
            dict.Clear();

            if (data == null)
            {
                return;
            }

            foreach (KeyValuePair<string, T> item in data)
            {
                dict[int.Parse(item.Key)] = item.Value;
            }

            if (typeof(T) == typeof(AbilityConfig))
            {
                nameDict = new Dictionary<string, T>();
                foreach (KeyValuePair<string, T> item in data)
                {
                    AbilityConfig abilityConfig = item.Value as AbilityConfig;
                    if (abilityConfig == null || string.IsNullOrEmpty(abilityConfig.KeyName))
                    {
                        continue;
                    }

                    nameDict[abilityConfig.KeyName] = item.Value;
                }
            }
        }

        public override void EndInit()
        {
        }

        public T Get(int id)
        {
            dict.TryGetValue(id, out T value);
            return value;
        }

        public T GetByName(string name)
        {
            if (nameDict == null)
            {
                return null;
            }

            nameDict.TryGetValue(name, out T value);
            return value;
        }

        public Dictionary<int, T> GetAll()
        {
            return dict;
        }

        public T GetOne()
        {
            return dict.Values.FirstOrDefault();
        }
    }
}
