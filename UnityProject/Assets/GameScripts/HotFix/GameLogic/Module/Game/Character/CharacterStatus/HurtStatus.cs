namespace GameLogic.Game
{
    public class HurtStatus : CharacterStatusBase
    {
        public HurtStatus(CharacterBase character) : base(character) { }

        public override void Enter()
        {
            base.Enter();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }
    }

    public class PlayerHurtStatus : HurtStatus
    {
        public PlayerHurtStatus(CharacterBase character) : base(character) { }
    }

    public class MonsterHurtStatus : HurtStatus
    {
        public MonsterHurtStatus(CharacterBase character) : base(character) { }
    }
}
