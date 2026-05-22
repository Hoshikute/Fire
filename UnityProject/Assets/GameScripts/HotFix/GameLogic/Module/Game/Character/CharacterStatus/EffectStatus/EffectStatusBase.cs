namespace GameLogic.Game
{
    public class EffectStatusBase
    {
        protected CharacterBase m_character;

        public virtual void Init(CharacterBase character)
        {
            m_character = character;
        }

        public virtual void Execute()
        {
        }
    }
}
