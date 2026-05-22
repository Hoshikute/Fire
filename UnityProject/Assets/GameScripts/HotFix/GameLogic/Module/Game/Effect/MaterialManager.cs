using UnityEngine;
using System.Collections.Generic;

namespace GameLogic.Game
{
    public static class MaterialManager
    {
        private static Dictionary<string, Material> s_materials = new Dictionary<string, Material>();

        public static Material GetMaterial(string name)
        {
            if (s_materials.TryGetValue(name, out Material mat))
            {
                return mat;
            }
            return null;
        }

        public static void CacheMaterial(string name, Material material)
        {
            if (!s_materials.ContainsKey(name))
            {
                s_materials.Add(name, material);
            }
        }

        public static void Clear()
        {
            s_materials.Clear();
        }
    }
}
