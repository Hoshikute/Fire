using GameLogic.SyncGameLogic.Component;
using UnityEngine;
using System.Collections.Generic;

namespace GameLogic.Game
{
    public class CharacterBase
    {
        public int m_characterID;
        public string m_characterName;
        public Camp m_camp;
        public CharacterBaseProperty m_Property;

        public Vector3 position { get; set; }
        public Vector3 forward { get; set; }

        public virtual void Init(string characterName, int characterID)
        {
            m_characterName = characterName;
            m_characterID = characterID;
        }

        public virtual void Dispose()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Move(Vector3 dir)
        {
            position += dir * Time.deltaTime * (m_Property?.m_movespeed ?? 5f);
        }

        public virtual void Skill(SkillCmd cmd)
        {
        }

        public virtual void Hurt(int damage)
        {
        }

        public virtual void Die()
        {
        }
    }

    public class SkillCmd
    {
        public int id;
        public string skillID;
        public Vector3 dir;
        public Vector3 pos;

        public void SetData(int _id, string _skillID, Vector3 _dir, Vector3 _pos)
        {
            id = _id;
            skillID = _skillID;
            dir = _dir;
            pos = _pos;
        }
    }
}
