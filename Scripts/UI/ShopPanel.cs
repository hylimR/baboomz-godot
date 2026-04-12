using Godot;

namespace Baboomz
{
    /// <summary>
    /// Campaign shop panel — spend currency on upgrades (Health, Damage, Armor).
    /// Reads/writes ProgressionService. Each upgrade has a max level and
    /// escalating cost. Currency display updates in real-time.
    /// </summary>
    public partial class ShopPanel : Control
    {
        private ProgressionService _progression;
        private Label _currencyLabel;
        private Label[] _upgradeLevelLabels;
        private Label[] _upgradeEffectLabels;
        private Button[] _buyButtons;

        private static readonly string[] UpgradeIds = { "health", "damage", "armor" };
        private static readonly string[] UpgradeNames = { "Health Boost", "Damage Up", "Armor Plating" };
        private static readonly string[] UpgradeDescriptions =
        {
            "+20 max HP per level",
            "+10% damage per level",
            "-5% damage taken per level"
        };
        private const int MaxUpgradeLevel = 5;

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            Visible = false;
            _progression = new ProgressionService();
            _upgradeLevelLabels = new Label[UpgradeIds.Length];
            _upgradeEffectLabels = new Label[UpgradeIds.Length];
            _buyButtons = new Button[UpgradeIds.Length];
            BuildUI();
        }

        private void BuildUI()
        {
            var overlay = new ColorRect();
            overlay.Name = "ShopOverlay";
            overlay.Color = new Color(0.05f, 0.04f, 0.08f, 0.97f);
            UIBuilder.SetAnchors(overlay, Vector2.Zero, Vector2.One);
            AddChild(overlay);

            UIBuilder.CreateLabel("SHOP", 36, UIBuilder.UiGold,
                overlay, new Vector2(0.35f, 0.01f), new Vector2(0.65f, 0.07f),
                HorizontalAlignment.Center);

            var backBtn = UIBuilder.CreateButton("ShopBack", "< Back", 18,
                new Color(0.5f, 0.3f, 0.3f), overlay);
            UIBuilder.SetAnchors(backBtn, new Vector2(0.02f, 0.02f), new Vector2(0.12f, 0.06f));
            backBtn.Pressed += () => Visible = false;

            // Currency display
            _currencyLabel = UIBuilder.CreateLabel("", 22,
                new Color(1f, 0.84f, 0f),
                overlay, new Vector2(0.65f, 0.02f), new Vector2(0.98f, 0.06f),
                HorizontalAlignment.Right);

            // Upgrade cards
            float cardY = 0.12f;
            float cardHeight = 0.22f;
            float cardGap = 0.04f;

            for (int i = 0; i < UpgradeIds.Length; i++)
            {
                int idx = i;
                float y = cardY + i * (cardHeight + cardGap);
                BuildUpgradeCard(overlay, idx, y, cardHeight);
            }
        }

        private void BuildUpgradeCard(Control parent, int idx, float y, float height)
        {
            var card = UIBuilder.CreatePanel($"UpgradeCard{idx}",
                new Color(0.12f, 0.1f, 0.16f, 0.9f), parent,
                new Vector2(0.1f, y), new Vector2(0.9f, y + height));

            // Name
            UIBuilder.CreateLabel(UpgradeNames[idx], 24, UIBuilder.UiGold,
                card, new Vector2(0.03f, 0.05f), new Vector2(0.5f, 0.3f),
                HorizontalAlignment.Left);

            // Description
            UIBuilder.CreateLabel(UpgradeDescriptions[idx], 14,
                new Color(0.7f, 0.7f, 0.7f),
                card, new Vector2(0.03f, 0.35f), new Vector2(0.5f, 0.55f),
                HorizontalAlignment.Left);

            // Current level
            _upgradeLevelLabels[idx] = UIBuilder.CreateLabel("", 18, Colors.White,
                card, new Vector2(0.55f, 0.05f), new Vector2(0.97f, 0.3f),
                HorizontalAlignment.Right);

            // Current effect
            _upgradeEffectLabels[idx] = UIBuilder.CreateLabel("", 14,
                new Color(0.5f, 0.8f, 0.5f),
                card, new Vector2(0.55f, 0.35f), new Vector2(0.97f, 0.55f),
                HorizontalAlignment.Right);

            // Buy button
            _buyButtons[idx] = UIBuilder.CreateButton($"Buy{UpgradeIds[idx]}", "BUY", 18,
                new Color(0.3f, 0.5f, 0.3f), card);
            UIBuilder.SetAnchors(_buyButtons[idx],
                new Vector2(0.3f, 0.65f), new Vector2(0.7f, 0.9f));
            _buyButtons[idx].Pressed += () => OnBuy(idx);
        }

        private void OnBuy(int idx)
        {
            int currentLevel = _progression.SaveData.GetUpgradeLevel(UpgradeIds[idx]);
            if (currentLevel >= MaxUpgradeLevel) return;

            int cost = GetUpgradeCost(currentLevel + 1);
            if (_progression.TryPurchaseUpgrade(UpgradeIds[idx], cost, MaxUpgradeLevel))
                RefreshUI();
        }

        private void RefreshUI()
        {
            _currencyLabel.Text = $"{_progression.SaveData.currency} coins";

            for (int i = 0; i < UpgradeIds.Length; i++)
            {
                int level = _progression.SaveData.GetUpgradeLevel(UpgradeIds[i]);
                bool maxed = level >= MaxUpgradeLevel;

                _upgradeLevelLabels[i].Text = $"Lv {level} / {MaxUpgradeLevel}";

                string effect = GetEffectText(i, level);
                _upgradeEffectLabels[i].Text = effect;

                if (maxed)
                {
                    _buyButtons[i].Text = "MAXED";
                    _buyButtons[i].Disabled = true;
                }
                else
                {
                    int cost = GetUpgradeCost(level + 1);
                    bool canAfford = _progression.SaveData.currency >= cost;
                    _buyButtons[i].Text = $"BUY ({cost})";
                    _buyButtons[i].Disabled = !canAfford;
                }
            }
        }

        private static string GetEffectText(int idx, int level)
        {
            if (level == 0) return "No bonus";
            return idx switch
            {
                0 => $"+{ProgressionService.CalculateHealthBonus(level):F0} HP",
                1 => $"x{ProgressionService.CalculateDamageMultiplier(level):F1} damage",
                2 => $"x{ProgressionService.CalculateArmorMultiplier(level):F2} damage taken",
                _ => ""
            };
        }

        private static int GetUpgradeCost(int targetLevel)
        {
            // Escalating: 100, 200, 400, 800, 1500
            return targetLevel switch
            {
                1 => 100,
                2 => 200,
                3 => 400,
                4 => 800,
                5 => 1500,
                _ => 9999
            };
        }

        public new void Show()
        {
            _progression.Reload();
            RefreshUI();
            Visible = true;
        }
    }
}
