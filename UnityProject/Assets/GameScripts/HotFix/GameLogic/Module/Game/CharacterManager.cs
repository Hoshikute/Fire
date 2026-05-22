using GameLogic.SyncGameLogic.Component;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Game
{
    public class CharacterManager
    {
        static List<CharacterBase> m_characterList = new List<CharacterBase>();

        public static List<CharacterBase> CharacterList
        {
            get { return m_characterList; }
        }

        public static CharacterBase CreateCharacter(CharacterTypeEnum characterType, string characterName, int characterID, Camp camp, Vector3 pos, Vector3 dir, float amplification)
        {
            // 简化版本，完整实现需要资源加载
            CharacterBase character = new CharacterBase();
            character.m_characterID = characterID;
            character.m_camp = camp;
            return character;
        }

        public static CharacterBase GetCharacter(int characterID)
        {
            for (int i = 0; i < m_characterList.Count; i++)
            {
                if (m_characterList[i].m_characterID == characterID)
                {
                    return m_characterList[i];
                }
            }
            return null;
        }

        public static bool GetCharacterIsExit(int characterID)
        {
            for (int i = 0; i < m_characterList.Count; i++)
            {
                if (m_characterList[i].m_characterID == characterID)
                {
                    return true;
                }
            }
            return false;
        }

        public static void RemoveCharacter(CharacterBase character)
        {
            if (m_characterList.Contains(character))
            {
                m_characterList.Remove(character);
            }
        }

        public static void Clear()
        {
            m_characterList.Clear();
        }
    }

    public enum CharacterTypeEnum
    {
        Brave,
        Monster,
        Trap,
        SkillToken
    }
}
