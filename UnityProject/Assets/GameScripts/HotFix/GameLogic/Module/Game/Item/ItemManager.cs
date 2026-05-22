using UnityEngine;
using System.Collections.Generic;

namespace GameLogic.Game
{
    public static class ItemManager
    {
        private static List<Item> s_items = new List<Item>();
        private static int s_nextItemID = 1;

        public static Item CreateItem(string itemName, Vector3 position)
        {
            Item item = new Item();
            item.Init(s_nextItemID++, itemName, position);
            s_items.Add(item);
            return item;
        }

        public static void RemoveItem(int itemID)
        {
            for (int i = s_items.Count - 1; i >= 0; i--)
            {
                if (s_items[i].itemID == itemID)
                {
                    s_items.RemoveAt(i);
                    break;
                }
            }
        }

        public static Item GetItem(int itemID)
        {
            foreach (var item in s_items)
            {
                if (item.itemID == itemID)
                    return item;
            }
            return null;
        }

        public static void Clear()
        {
            s_items.Clear();
            s_nextItemID = 1;
        }
    }
}
