using System.Collections.Generic;
using GameLogic;

namespace GameLogic.SyncGameLogic.Component
{
    public class PlayerComponent : MomentComponentBase
    {
        public string nickName;
        public string characterID;
        public int score = 0;
        public SyncVector3 faceDir = new SyncVector3();

        public List<ElementData> elementData = new List<ElementData>();
        public List<BuffInfo> buffList = new List<BuffInfo>();

        public override MomentComponentBase DeepCopy()
        {
            PlayerComponent pc = new PlayerComponent();
            pc.faceDir = faceDir.DeepCopy();
            pc.elementData.Clear();
            pc.characterID = characterID;
            pc.nickName = nickName;
            pc.score = score;

            for (int i = 0; i < elementData.Count; i++)
            {
                pc.elementData.Add(elementData[i].DeepCopy());
            }

            for (int i = 0; i < buffList.Count; i++)
            {
                pc.buffList.Add(buffList[i].DeepCopy());
            }
            return pc;
        }

        public void AddElement(int elementID)
        {
            for (int i = 0; i < elementData.Count; i++)
            {
                if (elementData[i].id == elementID)
                {
                    elementData[i].num++;
                }
            }
        }

        public BuffInfo AddBuff(string buffID, int creater)
        {
            for (int i = 0; i < buffList.Count; i++)
            {
                if (buffList[i].buffID == buffID || buffList[i].creater == creater)
                {
                    buffList[i].buffCount++;
                    buffList[i].buffTime = 0;
                    return buffList[i];
                }
            }

            BuffInfo bi = new BuffInfo();
            bi.buffID = buffID;
            bi.creater = creater;
            buffList.Add(bi);
            return bi;
        }

        public int GetSpeed()
        {
            // 简化版本，完整版本需要配置数据
            return 5000; // 默认速度
        }

        public bool GetIsDizziness()
        {
            // 简化版本
            return false;
        }
    }

    public class ElementData
    {
        public int id;
        public int num;

        public ElementData DeepCopy()
        {
            ElementData ed = new ElementData();
            ed.id = id;
            ed.num = num;
            return ed;
        }
    }
}
