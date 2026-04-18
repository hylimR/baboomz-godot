using Godot;

namespace Baboomz
{
    public partial class MainMenuSetup
    {
        private EncyclopediaPanel _encyclopediaPanel;
        private AchievementPanel _achievementPanel;
        private ShopPanel _shopPanel;
        private LoadoutPanel _loadoutPanel;

        private void BuildPanelButtons(ref float y)
        {
            // --- Encyclopedia button ---
            y += 0.08f;
            var encBtn = UIBuilder.CreateButton("EncyclopediaBtn", "Encyclopedia", 18,
                new Color(0.3f, 0.3f, 0.5f), this);
            UIBuilder.SetAnchors(encBtn, new Vector2(0.35f, y), new Vector2(0.65f, y + 0.05f));
            encBtn.Pressed += OnEncyclopediaPressed;

            _encyclopediaPanel = new EncyclopediaPanel();
            _encyclopediaPanel.Name = "EncyclopediaPanel";
            AddChild(_encyclopediaPanel);

            // --- Loadout button ---
            y += 0.07f;
            var loadoutBtn = UIBuilder.CreateButton("LoadoutBtn", "Loadout", 18,
                new Color(0.4f, 0.3f, 0.5f), this);
            UIBuilder.SetAnchors(loadoutBtn, new Vector2(0.35f, y), new Vector2(0.65f, y + 0.05f));
            loadoutBtn.Pressed += OnLoadoutPressed;

            _loadoutPanel = new LoadoutPanel();
            _loadoutPanel.Name = "LoadoutPanel";
            AddChild(_loadoutPanel);

            // --- Achievements button ---
            y += 0.07f;
            var achBtn = UIBuilder.CreateButton("AchievementsBtn", "Achievements", 18,
                new Color(0.4f, 0.35f, 0.2f), this);
            UIBuilder.SetAnchors(achBtn, new Vector2(0.35f, y), new Vector2(0.65f, y + 0.05f));
            achBtn.Pressed += OnAchievementsPressed;

            _achievementPanel = new AchievementPanel();
            _achievementPanel.Name = "AchievementPanel";
            AddChild(_achievementPanel);

            // --- Shop button ---
            y += 0.07f;
            var shopBtn = UIBuilder.CreateButton("ShopBtn", "Shop", 18,
                new Color(0.2f, 0.5f, 0.4f), this);
            UIBuilder.SetAnchors(shopBtn, new Vector2(0.35f, y), new Vector2(0.65f, y + 0.05f));
            shopBtn.Pressed += OnShopPressed;

            _shopPanel = new ShopPanel();
            _shopPanel.Name = "ShopPanel";
            AddChild(_shopPanel);
        }

        private void OnEncyclopediaPressed()
        {
            _encyclopediaPanel?.Show();
        }

        private void OnLoadoutPressed()
        {
            _loadoutPanel?.Show();
        }

        private void OnAchievementsPressed()
        {
            _achievementPanel?.Show();
        }

        private void OnShopPressed()
        {
            _shopPanel?.Show();
        }
    }
}
