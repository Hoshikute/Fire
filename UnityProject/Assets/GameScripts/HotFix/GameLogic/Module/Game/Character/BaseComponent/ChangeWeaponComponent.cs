namespace GameLogic.Game
{
    public class ChangeWeaponComponent : ComponentBase
    {
        private string m_currentWeapon;

        public string CurrentWeapon
        {
            get { return m_currentWeapon; }
        }

        public void ChangeWeapon(string weaponID)
        {
            m_currentWeapon = weaponID;
            // 切换武器逻辑
        }
    }
}
