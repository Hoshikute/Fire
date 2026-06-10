// ----------------------------------------------------------------
// 作者：HuHu <3112891874@qq.com>
// ----------------------------------------------------------------

using System;

namespace TEngine
{
    /// <summary>
    /// 可绑定属性，值变更时触发回调。
    /// 注意：帧同步逻辑层（GameLogic 命名空间）禁止使用（含闭包/事件，非确定性），仅在表现层使用。
    /// </summary>
    public class BindableProperty<T>
    {
        public Action<T> ValueChanged;
        private T m_value;

        public T Value
        {
            set
            {
                if (!Equals(m_value, value))
                {
                    m_value = value;
                    ValueChanged?.Invoke(m_value);
                }
            }
            get { return m_value; }
        }
    }
}
