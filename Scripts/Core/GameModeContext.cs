namespace Baboomz
{
    public enum GameMode
    {
        VsAI,
        PveCampaign,
        KingOfTheHill
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>
    /// Static cross-scene context carrier. Survives scene loads without
    /// autoload nodes or persistent singletons.
    /// Set before SceneTree.ChangeSceneToFile, read after scene loads.
    /// </summary>
    public static class GameModeContext
    {
        public static GameMode Mode { get; set; } = GameMode.VsAI;
        public static Difficulty SelectedDifficulty { get; set; } = Difficulty.Normal;
        public static string PlayerName { get; set; } = "Player1";
        public static string SelectedLevelId { get; set; } = "";
        public static PlayerLoadout Loadout { get; set; } = new PlayerLoadout();

        /// <summary>Skill index for slot 0 (default: teleport = 0).</summary>
        public static int SelectedSkillSlot0 { get; set; } = 0;
        /// <summary>Skill index for slot 1 (default: dash = 3).</summary>
        public static int SelectedSkillSlot1 { get; set; } = 3;

        public static void Reset()
        {
            Mode = GameMode.VsAI;
            SelectedLevelId = "";
            Loadout = new PlayerLoadout();
            SelectedSkillSlot0 = 0;
            SelectedSkillSlot1 = 3;
        }
    }

    /// <summary>
    /// Player's equipped weapons and upgrade state for a PVE level.
    /// Carried across scenes via GameModeContext.
    /// </summary>
    public class PlayerLoadout
    {
        /// <summary>Weapon IDs in slots 0-3. Null/empty = empty slot.</summary>
        public string[] weaponSlots = new string[4];

        /// <summary>Ammo remaining per slot. -1 = unlimited.</summary>
        public int[] ammo = new int[] { -1, -1, -1, -1 };

        /// <summary>Active slot index (0-3).</summary>
        public int activeSlot;

        /// <summary>Upgrade level for health (0-5).</summary>
        public int healthUpgradeLevel;

        /// <summary>Upgrade level for damage (0-5).</summary>
        public int damageUpgradeLevel;

        /// <summary>Upgrade level for armor (0-5).</summary>
        public int armorUpgradeLevel;
    }
}
