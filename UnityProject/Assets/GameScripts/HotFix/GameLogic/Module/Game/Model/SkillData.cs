namespace GameLogic.Game
{
    public class SkillData
    {
        public string m_skillID;
        public int Index;
        public float cd;
        public float damage;
        public string effectID;

        public SkillData DeepCopy()
        {
            SkillData sd = new SkillData();
            sd.m_skillID = m_skillID;
            sd.Index = Index;
            sd.cd = cd;
            sd.damage = damage;
            sd.effectID = effectID;
            return sd;
        }
    }
}
