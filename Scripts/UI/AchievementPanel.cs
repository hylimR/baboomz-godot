using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>
    /// Full-screen achievement panel accessible from the main menu.
    /// Tabs by category, shows unlocked/locked state, hidden achievements
    /// display "???" until unlocked. Counter: "X / 30 unlocked".
    /// </summary>
    public partial class AchievementPanel : Control
    {
        private Control _overlay;
        private VBoxContainer _achievementList;
        private Label _counterLabel;
        private Button[] _tabButtons;
        private int _activeTab;

        private static readonly string[] TabNames = { "Combat", "Skills", "Campaign", "Misc" };
        private static readonly AchievementCategory[] TabCategories =
        {
            AchievementCategory.Combat,
            AchievementCategory.Skill,
            AchievementCategory.Campaign,
            AchievementCategory.Misc
        };

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            Visible = false;
            BuildUI();
        }

        private void BuildUI()
        {
            _overlay = new ColorRect();
            _overlay.Name = "AchOverlay";
            ((ColorRect)_overlay).Color = new Color(0.05f, 0.04f, 0.08f, 0.97f);
            UIBuilder.SetAnchors(_overlay, Vector2.Zero, Vector2.One);
            AddChild(_overlay);

            // Title
            UIBuilder.CreateLabel("ACHIEVEMENTS", 36, UIBuilder.UiGold,
                _overlay, new Vector2(0.3f, 0.01f), new Vector2(0.7f, 0.07f),
                HorizontalAlignment.Center);

            // Back button
            var backBtn = UIBuilder.CreateButton("AchBack", "< Back", 18,
                new Color(0.5f, 0.3f, 0.3f), _overlay);
            UIBuilder.SetAnchors(backBtn, new Vector2(0.02f, 0.02f), new Vector2(0.12f, 0.06f));
            backBtn.Pressed += () => Visible = false;

            // Counter
            int total = AchievementDefs.All.Length;
            int unlocked = AchievementTracker.Unlocked.Count;
            _counterLabel = UIBuilder.CreateLabel($"{unlocked} / {total} unlocked", 18,
                new Color(0.8f, 0.8f, 0.8f),
                _overlay, new Vector2(0.7f, 0.02f), new Vector2(0.98f, 0.06f),
                HorizontalAlignment.Right);

            // Tab bar
            BuildTabBar();

            // Scrollable achievement list
            var listPanel = UIBuilder.CreatePanel("AchList",
                new Color(0.1f, 0.08f, 0.14f, 0.9f), _overlay,
                new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.96f));

            var scroll = new ScrollContainer();
            scroll.Name = "AchScroll";
            UIBuilder.SetAnchors(scroll, Vector2.Zero, Vector2.One);
            listPanel.AddChild(scroll);

            _achievementList = new VBoxContainer();
            _achievementList.Name = "AchEntries";
            scroll.AddChild(_achievementList);

            SwitchTab(0);
        }

        private void BuildTabBar()
        {
            _tabButtons = new Button[TabNames.Length];
            float tabW = 0.9f / TabNames.Length;
            float startX = 0.05f;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                var btn = UIBuilder.CreateButton($"AchTab{TabNames[i]}", TabNames[i], 14,
                    new Color(0.3f, 0.3f, 0.4f), _overlay);
                UIBuilder.SetAnchors(btn,
                    new Vector2(startX + tabW * i, 0.07f),
                    new Vector2(startX + tabW * (i + 1), 0.11f));
                btn.Pressed += () => SwitchTab(idx);
                _tabButtons[i] = btn;
            }
        }

        private void SwitchTab(int tabIndex)
        {
            _activeTab = tabIndex;

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                var style = new StyleBoxFlat();
                style.BgColor = i == tabIndex
                    ? new Color(0.25f, 0.2f, 0.1f)
                    : new Color(0.15f, 0.12f, 0.2f);
                style.SetCornerRadiusAll(2);
                if (i == tabIndex)
                {
                    style.SetBorderWidthAll(1);
                    style.BorderColor = UIBuilder.UiGold;
                }
                _tabButtons[i].AddThemeStyleboxOverride("normal", style);
                _tabButtons[i].AddThemeColorOverride("font_color",
                    i == tabIndex ? UIBuilder.UiGold : new Color(0.5f, 0.5f, 0.5f));
            }

            PopulateList(TabCategories[tabIndex]);
        }

        private void PopulateList(AchievementCategory category)
        {
            foreach (var child in _achievementList.GetChildren())
                if (child is Node n) n.QueueFree();

            foreach (var def in AchievementDefs.All)
            {
                if (def.Category != category) continue;

                bool unlocked = AchievementTracker.IsUnlocked(def.Id);
                bool hidden = def.IsHidden && !unlocked;

                var row = new HBoxContainer();
                row.CustomMinimumSize = new Vector2(0, 50);
                _achievementList.AddChild(row);

                // Status icon
                string icon = unlocked ? "[*]" : "[ ]";
                var iconLabel = new Label();
                iconLabel.Text = icon;
                iconLabel.AddThemeFontSizeOverride("font_size", 18);
                iconLabel.AddThemeColorOverride("font_color",
                    unlocked ? UIBuilder.UiGold : new Color(0.4f, 0.4f, 0.4f));
                iconLabel.CustomMinimumSize = new Vector2(50, 0);
                iconLabel.VerticalAlignment = VerticalAlignment.Center;
                row.AddChild(iconLabel);

                // Name + Description
                var textBox = new VBoxContainer();
                row.AddChild(textBox);

                var nameLabel = new Label();
                nameLabel.Text = hidden ? "???" : def.Name;
                nameLabel.AddThemeFontSizeOverride("font_size", 16);
                nameLabel.AddThemeColorOverride("font_color",
                    unlocked ? UIBuilder.UiGold : Colors.White);
                textBox.AddChild(nameLabel);

                var descLabel = new Label();
                descLabel.Text = hidden ? "Hidden achievement" : def.Description;
                descLabel.AddThemeFontSizeOverride("font_size", 12);
                descLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
                textBox.AddChild(descLabel);
            }
        }

        public new void Show()
        {
            // Refresh counter on show
            int total = AchievementDefs.All.Length;
            int unlocked = AchievementTracker.Unlocked.Count;
            _counterLabel.Text = $"{unlocked} / {total} unlocked";
            Visible = true;
            SwitchTab(_activeTab);
        }
    }
}
