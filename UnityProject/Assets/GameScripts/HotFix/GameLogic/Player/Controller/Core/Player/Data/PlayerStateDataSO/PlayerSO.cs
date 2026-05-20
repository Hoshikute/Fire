using UnityEngine;
namespace ThirdPersonController
{
    [CreateAssetMenu(menuName = "Asset/PlayerSO")]
    public class PlayerSO : ScriptableObject
    {
        [field:SerializeField]  public PlayerMovementData playerMovementData { get; private set; }
        [field :SerializeField] public PlayerParameterData playerParameterData { get; private set; }
    }
}
