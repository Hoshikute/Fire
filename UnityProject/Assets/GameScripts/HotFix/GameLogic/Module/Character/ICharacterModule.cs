using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic.Character
{
    public interface ICharacterModule
    {
        string ThirdPersonPlayerLocation { get; }
        string ThirdPersonPlayerPackageName { get; }
        GameObject ThirdPersonPlayerInstance { get; }

        void SetThirdPersonPlayerPrefab(string location, string packageName = "");
        UniTask<GameObject> LoadThirdPersonPlayerAsync(Transform parent = null, CancellationToken cancellationToken = default);
        void DestroyThirdPersonPlayer();
    }
}
