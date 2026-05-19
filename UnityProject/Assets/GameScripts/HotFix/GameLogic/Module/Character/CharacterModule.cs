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

        public string ThirdPersonPlayerLocation { get; private set; }
        public string ThirdPersonPlayerPackageName { get; private set; }
        public GameObject ThirdPersonPlayerInstance { get; private set; }

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

            DestroyThirdPersonPlayer();

            ThirdPersonPlayerLocation = null;
            ThirdPersonPlayerPackageName = null;
            _isLoading = false;
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        public void SetThirdPersonPlayerPrefab(string location, string packageName = "")
        {
            Log.Assert(!string.IsNullOrEmpty(location), "[CharacterModule] location is null or empty.");
            ThirdPersonPlayerLocation = location;
            ThirdPersonPlayerPackageName = packageName ?? string.Empty;
        }

        public UniTask<GameObject> LoadThirdPersonPlayerAsync(Transform parent = null, CancellationToken cancellationToken = default)
        {
            Log.Assert(!_isShutdown, "[CharacterModule] Module has been shutdown.");

            if (ThirdPersonPlayerInstance != null)
            {
                return UniTask.FromResult(ThirdPersonPlayerInstance);
            }

            if (_isLoading)
            {
                return _loadingTask;
            }

            Log.Assert(!string.IsNullOrEmpty(ThirdPersonPlayerLocation), "[CharacterModule] ThirdPersonPlayerLocation is null or empty.");

            _isLoading = true;

            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _moduleCts.Token);

            _loadingTask = DoLoadThirdPersonPlayerAsync(parent, _loadCts.Token);
            return _loadingTask;
        }

        private async UniTask<GameObject> DoLoadThirdPersonPlayerAsync(Transform parent, CancellationToken cancellationToken)
        {
            try
            {
                GameObject instance = await GameModule.Resource.LoadGameObjectAsync(
                    ThirdPersonPlayerLocation,
                    parent,
                    cancellationToken,
                    ThirdPersonPlayerPackageName
                );

                if (_isShutdown || cancellationToken.IsCancellationRequested)
                {
                    if (instance != null)
                    {
                        Object.Destroy(instance);
                    }

                    return null;
                }

                ThirdPersonPlayerInstance = instance;
                return instance;
            }
            finally
            {
                _isLoading = false;
            }
        }

        public void DestroyThirdPersonPlayer()
        {
            if (ThirdPersonPlayerInstance == null)
            {
                return;
            }

            Object.Destroy(ThirdPersonPlayerInstance);
            ThirdPersonPlayerInstance = null;
        }
    }
}
