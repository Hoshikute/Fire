using System;

namespace ET
{
    /// <summary>
    /// Battle legacy config compatibility base attribute.
    /// 仅供旧版 EGamePlay 配置类型在 GameProto 程序集中编译与反射识别使用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BaseAttribute : Attribute
    {
        public Type AttributeType { get; }

        public BaseAttribute()
        {
            AttributeType = GetType();
        }
    }
}
