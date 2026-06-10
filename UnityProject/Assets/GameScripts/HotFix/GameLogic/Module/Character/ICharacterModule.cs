using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic.Character
{
    public interface ICharacterModule
    {
        string CharacterLocation { get; }
        string CharacterPackageName { get; }
        GameObject CharacterInstance { get; }

        void SetCharacterPrefab(string location, string packageName = "");
        UniTask<GameObject> LoadCharacterAsync(Transform parent = null, CancellationToken cancellationToken = default);
        void DestroyCharacter();
    }
}
