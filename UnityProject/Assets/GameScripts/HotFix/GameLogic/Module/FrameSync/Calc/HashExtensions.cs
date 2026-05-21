namespace GameLogic
{
    /// <summary>
    /// 字符串哈希扩展
    /// </summary>
    public static class StringHashExtensions
    {
        /// <summary>
        /// 将字符串转换为稳定的哈希值（用于实体ID生成）
        /// </summary>
        public static int ToHash(this string str)
        {
            unchecked
            {
                int hash = 5381;
                for (int i = 0; i < str.Length; i++)
                {
                    hash = ((hash << 5) + hash) + str[i];
                }
                return hash;
            }
        }
    }
}
