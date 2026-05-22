using UnityEngine;

namespace GameLogic.Game
{
    public class SkillTokenMoveStatus : CharacterStatusBase
    {
        private float m_speed = 20f;

        public SkillTokenMoveStatus(CharacterBase character) : base(character) { }

        public override void Update()
        {
            base.Update();
            if (m_character != null)
            {
                m_character.position += m_character.forward * m_speed * Time.deltaTime;
            }
        }
    }
}
