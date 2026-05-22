namespace GameLogic.Game
{
    public class BraveProperty : CharacterBaseProperty
    {
        public BraveProperty(string characterName)
        {
            m_modelID = characterName;
            m_movespeed = 6f;
            m_maxHP = 150;
            m_attack = 15;
            m_defense = 8;
        }
    }
}
