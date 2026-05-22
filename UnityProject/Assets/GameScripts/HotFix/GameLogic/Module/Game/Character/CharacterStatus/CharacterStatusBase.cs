namespace GameLogic.Game
{
    public interface ICharacterStatus
    {
        void Enter();
        void Update();
        void Exit();
    }

    public class CharacterStatusBase : ICharacterStatus
    {
        protected CharacterBase m_character;

        public CharacterStatusBase(CharacterBase character)
        {
            m_character = character;
        }

        public virtual void Enter()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Exit()
        {
        }
    }
}
