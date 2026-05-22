using UnityEngine;

namespace GameLogic.Game
{
    public class SkillToken : CharacterBase
    {
        private string m_skillID;
        private Vector3 m_dir;

        public override void Init(string characterName, int characterID)
        {
            base.Init(characterName, characterID);
        }

        public void SetSkillData(string skillID, Vector3 dir)
        {
            m_skillID = skillID;
            m_dir = dir;
        }

        public override void Update()
        {
            base.Update();
            // 飞行物移动逻辑
        }
    }
}
