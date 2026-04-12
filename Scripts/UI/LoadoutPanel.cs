using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Campaign loadout panel — set starting weapon, view equipped skills,
    /// and see current upgrade bonuses. Accessible from the main menu.
    /// </summary>
    public partial class LoadoutPanel : Control
    {
        private ProgressionService _progression;
        private Label _startWeaponLabel;
        private Label _skill0Label;
        private Label _skill1Label;
        private Label _upgradesLabel;
        private int _startWeaponIndex;

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            Visible = false;
            _progression = new ProgressionService();
            _startWeaponIndex = GameModeContext.StartWeaponSlot;
            BuildUI();
        }

        private void BuildUI()
        {
            var overlay = new ColorRect();
            overlay.Name = "LoadoutOverlay";
            overlay.Color = new Color(0.05f, 0.04f, 0.08f, 0.97f);
            UIBuilder.SetAnchors(overlay, Vector2.Zero, Vector2.One);
            AddChild(overlay);

            UIBuilder.CreateLabel("LOADOUT", 36, UIBuilder.UiGold,
                overlay, new Vector2(0.3f, 0.01f), new Vector2(0.7f, 0.07f),
                HorizontalAlignment.Center);

            var backBtn = UIBuilder.CreateButton("LoadoutBack", "< Back", 18,
                new Color(0.5f, 0.3f, 0.3f), overlay);
            UIBuilder.SetAnchors(backBtn, new Vector2(0.02f, 0.02f), new Vector2(0.12f, 0.06f));
            backBtn.Pressed += () => Visible = false;

            // Starting weapon section
            var weaponCard = UIBuilder.CreatePanel("WeaponCard",
                new Color(0.12f, 0.1f, 0.16f, 0.9f), overlay,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.32f));

            UIBuilder.CreateLabel("Starting Weapon", 22, UIBuilder.UiGold,
                weaponCard, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.25f),
                HorizontalAlignment.Center);

            UIBuilder.CreateLabel("Choose which weapon you start the match with",
                13, new Color(0.6f, 0.6f, 0.6f),
                weaponCard, new Vector2(0.03f, 0.25f), new Vector2(0.97f, 0.4f),
                HorizontalAlignment.Center);

            var leftBtn = UIBuilder.CreateButton("WeaponLeft", "<", 24,
                new Color(0.3f, 0.3f, 0.4f), weaponCard);
            UIBuilder.SetAnchors(leftBtn, new Vector2(0.15f, 0.5f), new Vector2(0.25f, 0.85f));
            leftBtn.Pressed += () => CycleWeapon(-1);

            _startWeaponLabel = UIBuilder.CreateLabel("", 22,
                new Color(0.9f, 0.85f, 0.7f),
                weaponCard, new Vector2(0.25f, 0.5f), new Vector2(0.75f, 0.85f),
                HorizontalAlignment.Center);
            _startWeaponLabel.VerticalAlignment = VerticalAlignment.Center;

            var rightBtn = UIBuilder.CreateButton("WeaponRight", ">", 24,
                new Color(0.3f, 0.3f, 0.4f), weaponCard);
            UIBuilder.SetAnchors(rightBtn, new Vector2(0.75f, 0.5f), new Vector2(0.85f, 0.85f));
            rightBtn.Pressed += () => CycleWeapon(1);

            // Skills section
            var skillCard = UIBuilder.CreatePanel("SkillCard",
                new Color(0.12f, 0.1f, 0.16f, 0.9f), overlay,
                new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.55f));

            UIBuilder.CreateLabel("Equipped Skills", 22, UIBuilder.UiGold,
                skillCard, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.35f),
                HorizontalAlignment.Center);

            _skill0Label = UIBuilder.CreateLabel("", 16, new Color(0.7f, 0.8f, 1f),
                skillCard, new Vector2(0.1f, 0.45f), new Vector2(0.5f, 0.85f),
                HorizontalAlignment.Center);
            _skill0Label.VerticalAlignment = VerticalAlignment.Center;

            _skill1Label = UIBuilder.CreateLabel("", 16, new Color(0.7f, 0.8f, 1f),
                skillCard, new Vector2(0.5f, 0.45f), new Vector2(0.9f, 0.85f),
                HorizontalAlignment.Center);
            _skill1Label.VerticalAlignment = VerticalAlignment.Center;

            // Upgrades section
            var upgradeCard = UIBuilder.CreatePanel("UpgradeCard",
                new Color(0.12f, 0.1f, 0.16f, 0.9f), overlay,
                new Vector2(0.1f, 0.59f), new Vector2(0.9f, 0.85f));

            UIBuilder.CreateLabel("Active Upgrades", 22, UIBuilder.UiGold,
                upgradeCard, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.25f),
                HorizontalAlignment.Center);

            _upgradesLabel = UIBuilder.CreateLabel("", 15, new Color(0.5f, 0.8f, 0.5f),
                upgradeCard, new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.95f),
                HorizontalAlignment.Left);
            _upgradesLabel.VerticalAlignment = VerticalAlignment.Top;
        }

        private void CycleWeapon(int direction)
        {
            var config = new GameConfig();
            int count = config.Weapons.Length;
            _startWeaponIndex = (_startWeaponIndex + direction + count) % count;
            GameModeContext.StartWeaponSlot = _startWeaponIndex;
            RefreshUI();
        }

        private void RefreshUI()
        {
            var config = new GameConfig();

            // Starting weapon
            string weaponId = config.Weapons[_startWeaponIndex].WeaponId;
            _startWeaponLabel.Text = weaponId?.Replace('_', ' ') ?? "unknown";

            // Skills
            int s0 = GameModeContext.SelectedSkillSlot0;
            int s1 = GameModeContext.SelectedSkillSlot1;
            _skill0Label.Text = $"Q: {GetSkillName(config, s0)}";
            _skill1Label.Text = $"E: {GetSkillName(config, s1)}";

            // Upgrades
            var save = _progression.SaveData;
            int hpLvl = save.GetUpgradeLevel("health");
            int dmgLvl = save.GetUpgradeLevel("damage");
            int armLvl = save.GetUpgradeLevel("armor");

            string upgradeText = "";
            if (hpLvl > 0)
                upgradeText += $"  Health Boost Lv{hpLvl}: +{ProgressionService.CalculateHealthBonus(hpLvl):F0} HP\n";
            if (dmgLvl > 0)
                upgradeText += $"  Damage Up Lv{dmgLvl}: x{ProgressionService.CalculateDamageMultiplier(dmgLvl):F1}\n";
            if (armLvl > 0)
                upgradeText += $"  Armor Plating Lv{armLvl}: x{ProgressionService.CalculateArmorMultiplier(armLvl):F2}\n";
            if (string.IsNullOrEmpty(upgradeText))
                upgradeText = "  No upgrades purchased yet — visit the Shop!";

            _upgradesLabel.Text = upgradeText;
        }

        private static string GetSkillName(GameConfig config, int index)
        {
            if (index < 0 || index >= config.Skills.Length) return "???";
            return config.Skills[index].SkillId?.Replace('_', ' ') ?? "unknown";
        }

        public new void Show()
        {
            _progression.Reload();
            _startWeaponIndex = GameModeContext.StartWeaponSlot;
            RefreshUI();
            Visible = true;
        }
    }
}
