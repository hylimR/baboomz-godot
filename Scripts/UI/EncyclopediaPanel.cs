using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Full-screen encyclopedia panel accessible from the main menu.
    /// Tabs: Weapons | Skills | Mobs | Bosses | Biomes | Lore
    /// Left column: scrollable entry list. Right column: detail view.
    /// </summary>
    public partial class EncyclopediaPanel : Control
    {
        private Control _overlay;
        private VBoxContainer _entryList;
        private Label _detailTitle;
        private Label _detailDescription;
        private Label _detailStats;
        private Button[] _tabButtons;
        private int _activeTab;

        private static readonly string[] TabNames =
            { "Weapons", "Skills", "Mobs", "Bosses", "Biomes", "Lore" };
        private static readonly Color TabActiveColor = UIBuilder.UiGold;
        private static readonly Color TabInactiveColor = new Color(0.5f, 0.5f, 0.5f);

        private EncyclopediaEntry[][] _tabEntries;

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            Visible = false;
            LoadEntries();
            BuildUI();
        }

        private void LoadEntries()
        {
            var config = new GameConfig();
            _tabEntries = new[]
            {
                EncyclopediaData.GetWeaponEntries(config),
                EncyclopediaData.GetSkillEntries(config),
                EncyclopediaData.GetMobEntries(),
                EncyclopediaData.GetBossEntries(),
                EncyclopediaData.GetBiomeEntries(),
                MergeLoreEntries()
            };
        }

        private static EncyclopediaEntry[] MergeLoreEntries()
        {
            var factions = EncyclopediaData.GetFactionEntries();
            var history = EncyclopediaData.GetHistoryEntries();
            var merged = new EncyclopediaEntry[factions.Length + history.Length];
            factions.CopyTo(merged, 0);
            history.CopyTo(merged, factions.Length);
            return merged;
        }

        private void BuildUI()
        {
            // Full-screen dark overlay
            _overlay = new ColorRect();
            _overlay.Name = "EncOverlay";
            ((ColorRect)_overlay).Color = new Color(0.05f, 0.04f, 0.08f, 0.97f);
            UIBuilder.SetAnchors(_overlay, Vector2.Zero, Vector2.One);
            AddChild(_overlay);

            // Title
            var title = UIBuilder.CreateLabel("ENCYCLOPEDIA", 36, UIBuilder.UiGold,
                _overlay, new Vector2(0.3f, 0.01f), new Vector2(0.7f, 0.07f),
                HorizontalAlignment.Center);
            title.VerticalAlignment = VerticalAlignment.Center;

            // Back button
            var backBtn = UIBuilder.CreateButton("EncBack", "< Back", 18,
                new Color(0.5f, 0.3f, 0.3f), _overlay);
            UIBuilder.SetAnchors(backBtn, new Vector2(0.02f, 0.02f), new Vector2(0.12f, 0.06f));
            backBtn.Pressed += () => Visible = false;

            // Tab bar
            BuildTabBar();

            // Left panel: entry list (scrollable)
            var leftPanel = UIBuilder.CreatePanel("LeftPanel",
                new Color(0.1f, 0.08f, 0.14f, 0.9f), _overlay,
                new Vector2(0.02f, 0.12f), new Vector2(0.3f, 0.96f));

            var scroll = new ScrollContainer();
            scroll.Name = "EntryScroll";
            UIBuilder.SetAnchors(scroll, Vector2.Zero, Vector2.One);
            leftPanel.AddChild(scroll);

            _entryList = new VBoxContainer();
            _entryList.Name = "EntryList";
            scroll.AddChild(_entryList);

            // Right panel: detail view
            var rightPanel = UIBuilder.CreatePanel("RightPanel",
                new Color(0.12f, 0.1f, 0.16f, 0.9f), _overlay,
                new Vector2(0.32f, 0.12f), new Vector2(0.98f, 0.96f));

            _detailTitle = UIBuilder.CreateLabel("", 28, UIBuilder.UiGold,
                rightPanel, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.1f),
                HorizontalAlignment.Left);
            _detailTitle.VerticalAlignment = VerticalAlignment.Center;

            _detailDescription = UIBuilder.CreateLabel("", 15, new Color(0.85f, 0.85f, 0.85f),
                rightPanel, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.5f),
                HorizontalAlignment.Left);
            _detailDescription.VerticalAlignment = VerticalAlignment.Top;
            _detailDescription.AutowrapMode = TextServer.AutowrapMode.WordSmart;

            _detailStats = UIBuilder.CreateLabel("", 14, new Color(0.7f, 0.8f, 0.9f),
                rightPanel, new Vector2(0.03f, 0.52f), new Vector2(0.97f, 0.96f),
                HorizontalAlignment.Left);
            _detailStats.VerticalAlignment = VerticalAlignment.Top;

            // Show first tab
            SwitchTab(0);
        }

        private void BuildTabBar()
        {
            _tabButtons = new Button[TabNames.Length];
            float tabW = 1f / TabNames.Length;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int tabIdx = i;
                var btn = UIBuilder.CreateButton($"Tab{TabNames[i]}", TabNames[i], 14,
                    TabInactiveColor, _overlay);
                UIBuilder.SetAnchors(btn,
                    new Vector2(tabW * i, 0.07f),
                    new Vector2(tabW * (i + 1), 0.11f));
                btn.Pressed += () => SwitchTab(tabIdx);
                _tabButtons[i] = btn;
            }
        }

        public new void Show()
        {
            Visible = true;
            SwitchTab(_activeTab);
        }
    }
}
