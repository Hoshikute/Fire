using Cysharp.Threading.Tasks;
using EGamePlay;
using EGamePlay.Combat;
using GameLogic.Battle.Config;
using TEngine;
using UnityEngine;

namespace GameLogic.Battle
{
    public sealed class BattleModule : Module, IBattleModule, IUpdateModule
    {
        private const string ConfigsPrefabPath = "Configs";

        public override int Priority => 10;

        public CombatContext Context { get; private set; }

        public bool IsInitialized { get; private set; }

        // 正在初始化中的标志，防止重复初始化
        private bool _isInitializing;

        // 模块引用（GameBattle 程序集无法访问 GameModule，使用 ModuleSystem）
        private IResourceModule ResourceModule => ModuleSystem.GetModule<IResourceModule>();

        public override void OnInit()
        {
            // 异步初始化
            InitializeAsync().Forget();
        }

        public void EnsureInitialized()
        {
            // 接口要求：同步初始化检查
            if (IsInitialized || _isInitializing)
            {
                return;
            }

            Entity.EnableLog = false;

            ECSNode ecsNode = ECSNode.Create();
            Context = ecsNode.AddChild<CombatContext>();

            // 同步加载配置 Prefab（需要资源已预加载）
            GameObject configsPrefab = ResourceModule.LoadGameObject(ConfigsPrefabPath);
            ReferenceCollector collector = configsPrefab != null ? configsPrefab.GetComponent<ReferenceCollector>() : null;
            ecsNode.AddComponent<ConfigManageComponent>(collector);

            IsInitialized = true;
            TEngine.Log.Info("[BattleModule] Initialized (sync)");
        }

        private async UniTaskVoid InitializeAsync()
        {
            // 防止重复初始化
            if (IsInitialized || _isInitializing)
            {
                return;
            }

            _isInitializing = true;

            Entity.EnableLog = false;

            ECSNode ecsNode = ECSNode.Create();
            Context = ecsNode.AddChild<CombatContext>();

            // 使用 ResourceModule 异步加载配置 Prefab
            GameObject configsPrefab = await ResourceModule.LoadGameObjectAsync(ConfigsPrefabPath);
            ReferenceCollector collector = configsPrefab != null ? configsPrefab.GetComponent<ReferenceCollector>() : null;
            ecsNode.AddComponent<ConfigManageComponent>(collector);

            IsInitialized = true;
            _isInitializing = false;
            TEngine.Log.Info("[BattleModule] Initialized (async)");
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (!IsInitialized)
            {
                return;
            }

            ECSNode.Instance?.Update();
            ECSNode.Instance?.FixedUpdate();
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            ECSNode.Destroy();
            Context = null;
            IsInitialized = false;
            _isInitializing = false;
            TEngine.Log.Info("[BattleModule] Shutdown");
        }
    }
}
