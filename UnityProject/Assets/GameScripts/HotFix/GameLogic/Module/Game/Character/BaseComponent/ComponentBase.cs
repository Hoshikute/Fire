namespace GameLogic.Game
{
    public class ComponentBase
    {
        protected CharacterBase m_character;

        public CharacterBase Character
        {
            get { return m_character; }
            set { m_character = value; }
        }

        public virtual void Init(CharacterBase character)
        {
            m_character = character;
        }

        public virtual void Dispose()
        {
            m_character = null;
        }

        public virtual void Update()
        {
        }
    }
}
