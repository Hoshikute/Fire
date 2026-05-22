using UnityEngine;

namespace GameLogic.Game
{
    public class Item
    {
        public int itemID;
        public string itemName;
        public Vector3 position;
        public bool isActive = true;

        public virtual void Init(int id, string name, Vector3 pos)
        {
            itemID = id;
            itemName = name;
            position = pos;
        }

        public virtual void OnPickup(CharacterBase character)
        {
            isActive = false;
        }
    }
}
