using EGamePlay.Combat;

namespace GameLogic.Battle
{
    public interface IBattleModule
    {
        CombatContext Context { get; }
        bool IsInitialized { get; }
        void EnsureInitialized();
    }
}
