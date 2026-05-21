namespace GameLogic
{
    /// <summary>
    /// 玩家命令基类
    /// </summary>
    public abstract class PlayerCommandBase : ComponentBase
    {
        public int id;
        public int frame;
        public int time;

        public abstract PlayerCommandBase DeepCopy();
        public abstract bool EqualsCmd(PlayerCommandBase cmd);
    }
}
