using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic.Character
{
    public sealed class CharacterModule : Module, ICharacterModule, IUpdateModule
    {
        private CancellationTokenSource _moduleCts;
        private CancellationTokenSource _loadCts;
        private bool _isLoading;
        private bool _isShutdown;
        private UniTask<GameObject> _loadingTask;

        public string CharacterLocation { get; private set; }
        public string CharacterPackageName { get; private set; }
        public GameObject CharacterInstance { get; private set; }

        public override void OnInit()
        {
            _moduleCts = new CancellationTokenSource();
            _isShutdown = false;
        }

        public override void Shutdown()
        {
            _isShutdown = true;

            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;

            _moduleCts?.Cancel();
            _moduleCts?.Dispose();
            _moduleCts = null;

            DestroyCharacter();

            CharacterLocation = null;
            CharacterPackageName = null;
            _isLoading = false;
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        public void SetCharacterPrefab(string location, string packageName = "")
        {
            Log.Assert(!string.IsNullOrEmpty(location), "[CharacterModule] location is null or empty.");
            CharacterLocation = location;
            CharacterPackageName = packageName ?? string.Empty;
        }

        public UniTask<GameObject> LoadCharacterAsync(Transform parent = null, CancellationToken cancellationToken = default)
        {
            Log.Assert(!_isShutdown, "[CharacterModule] Module has been shutdown.");

            if (CharacterInstance != null)
            {
                return UniTask.FromResult(CharacterInstance);
            }

            if (_isLoading)
            {
                return _loadingTask;
            }

            Log.Assert(!string.IsNullOrEmpty(CharacterLocation), "[CharacterModule] CharacterLocation is null or empty.");

            _isLoading = true;

            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _moduleCts.Token);

            _loadingTask = DoLoadCharacterAsync(parent, _loadCts.Token);
            return _loadingTask;
        }

        private async UniTask<GameObject> DoLoadCharacterAsync(Transform parent, CancellationToken cancellationToken)
        {
            try
            {
                GameObject instance = await GameModule.Resource.LoadGameObjectAsync(
                    CharacterLocation,
                    parent,
                    cancellationToken,
                    CharacterPackageName
                );

                if (_isShutdown || cancellationToken.IsCancellationRequested)
                {
                    if (instance != null)
                    {
                        Object.Destroy(instance);
                    }

                    return null;
                }

                CharacterInstance = instance;
                return instance;
            }
            finally
            {
                _isLoading = false;
            }
        }

        public void DestroyCharacter()
        {
            if (CharacterInstance == null)
            {
                return;
            }

            Object.Destroy(CharacterInstance);
            CharacterInstance = null;
        }
    }
}
