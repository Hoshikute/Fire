using UnityEngine;

namespace GameLogic.Game
{
    public class EffectComponent : ComponentBase
    {
        public void CreateEffectInCharacter(string effectName, HardPointEnum hardPoint, float duration = 0f)
        {
            // 创建特效
        }

        public void CreateEffectAtPosition(string effectName, Vector3 position, float duration = 0f)
        {
            // 在指定位置创建特效
        }

        public void RemoveEffect(string effectName)
        {
            // 移除特效
        }
    }

    public enum HardPointEnum
    {
        position,
        waist,
        leftHand,
        rightHand,
        head
    }
}
