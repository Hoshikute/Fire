namespace GameLogic.FrameSync
{
    /// <summary>
    /// 帧同步资源地址常量
    /// 资源通过 YooAsset 按 location（文件名）寻址
    /// </summary>
    public static class FrameSyncAssetKey
    {
        // === 角色预制体 ===
        public const string Hero_01 = "hero_01";
        public const string Male_01 = "male_01";
        public const string Female_01 = "famale_01";
        public const string War_Male_02 = "war_male_02";
        public const string War_Female_03 = "war_female_03";
        public const string GhostObj = "GhostObj";

        // === 特效预制体 ===
        // Drop 特效
        public const string EFX_Atk_001 = "EFX_atk_001";
        public const string EFX_Bolt_001 = "EFX_bolt_001";
        public const string EFX_Fire_001 = "EFX_fire_001";
        public const string EFX_Firesoil_001 = "EFX_firesoil_001";

        // Buff 特效
        public const string Buff_Jiansu = "jiansu_buff";
        public const string Buff_Ranshao = "ranshao_buff";
        public const string Buff_Xuanyun = "xuanyun_buff";
        public const string Buff_Yishang = "yishang_buff";

        // === 配置文件 ===
        public const string Config_SkillData = "SkillData";
        public const string Config_BuffData = "BuffData";
        public const string Config_PlayerData = "PlayerData";
        public const string Config_AreaData = "AreaData";
        public const string Config_ItemsData = "ItemsData";
        public const string Config_WeaponData = "WeaponData";
        public const string Config_ShiftData = "ShiftData";
        public const string Config_FlyData = "FlyData";
        public const string Config_MonsterData = "MonsterData";
        public const string Config_DropData = "DropData";
        public const string Config_ConstantData = "ConstantData";

        // === 场景元素 ===
        public const string CameraRoot = "CameraRoot";
        public const string Sence = "Sence";
        public const string AreaTips = "AreaTips";
    }
}
