using System.Collections.Generic;
using UnityEngine;

namespace EGamePlay.Combat
{
    internal static class BattleToolsEditorExtensions
    {
        public static void SetLength<T>(this List<T> list, int length)
        {
            if (list == null)
            {
                return;
            }

            while (list.Count < length)
            {
                list.Add(default);
            }

            while (list.Count > length)
            {
                list.RemoveAt(list.Count - 1);
            }
        }

        public static Rect Padding(this Rect rect, float padding)
        {
            return new Rect(
                rect.x + padding,
                rect.y + padding,
                rect.width - padding * 2f,
                rect.height - padding * 2f);
        }
    }
}
