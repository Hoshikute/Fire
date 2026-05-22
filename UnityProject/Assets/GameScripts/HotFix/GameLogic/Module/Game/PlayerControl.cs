using UnityEngine;

namespace GameLogic.Game
{
    public class PlayerControl
    {
        private CharacterBase character;
        public int m_targetCharacterID;

        public CharacterBase ControlCharacter
        {
            get { return character; }
            set { character = value; }
        }

        public void Init()
        {
        }

        public void Dispose()
        {
            character = null;
        }

        public void Update()
        {
        }
    }
}
