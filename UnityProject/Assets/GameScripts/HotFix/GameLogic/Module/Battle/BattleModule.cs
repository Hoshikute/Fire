using System.Threading;
using EGamePlay;
using EGamePlay.Combat;
using ET;
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

        public override void OnInit()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (IsInitialized)
            {
                return;
            }

            SynchronizationContext.SetSynchronizationContext(ThreadSynchronizationContext.Instance);
            Entity.EnableLog = false;

            ECSNode ecsNode = ECSNode.Create();
            ecsNode.AddChild<TimerManager>();
            Context = ecsNode.AddChild<CombatContext>();

            GameObject configsPrefab = Resources.Load<GameObject>(ConfigsPrefabPath);
            ReferenceCollector collector = configsPrefab != null ? configsPrefab.GetComponent<ReferenceCollector>() : null;
            ecsNode.AddComponent<ConfigManageComponent>(collector);

            IsInitialized = true;
            TEngine.Log.Info("[BattleModule] Initialized");
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (!IsInitialized)
            {
                return;
            }

            ThreadSynchronizationContext.Instance.Update();
            ECSNode.Instance?.Update();
            TimerManager.Instance?.Update();
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
            TEngine.Log.Info("[BattleModule] Shutdown");
        }
    }
}
