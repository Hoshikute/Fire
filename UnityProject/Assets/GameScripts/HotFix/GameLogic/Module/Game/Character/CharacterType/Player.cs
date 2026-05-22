namespace GameLogic.Game
{
    public class Player : CharacterBase
    {
        public override void Init(string characterName, int characterID)
        {
            base.Init(characterName, characterID);
            m_Property = new BraveProperty(characterName);
        }
    }
}
