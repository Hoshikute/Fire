using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Game
{
    public static class FlyObjectManager
    {
        private static List<FlyObjectBase> s_flyObjects = new List<FlyObjectBase>();

        public static void AddFlyObject(FlyObjectBase flyObject)
        {
            s_flyObjects.Add(flyObject);
        }

        public static void RemoveFlyObject(FlyObjectBase flyObject)
        {
            s_flyObjects.Remove(flyObject);
        }

        public static void Update()
        {
            for (int i = s_flyObjects.Count - 1; i >= 0; i--)
            {
                s_flyObjects[i].Update();
            }
        }

        public static void Clear()
        {
            s_flyObjects.Clear();
        }
    }
}
