using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Tab switching and entry rendering for the encyclopedia panel.
    /// </summary>
    public partial class EncyclopediaPanel
    {
        private void SwitchTab(int tabIndex)
        {
            _activeTab = tabIndex;

            // Update tab button styles
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                var color = i == tabIndex ? TabActiveColor : TabInactiveColor;
                var style = new StyleBoxFlat();
                style.BgColor = i == tabIndex
                    ? new Color(0.25f, 0.2f, 0.1f)
                    : new Color(0.15f, 0.12f, 0.2f);
                style.SetCornerRadiusAll(2);
                if (i == tabIndex)
                {
                    style.SetBorderWidthAll(1);
                    style.BorderColor = TabActiveColor;
                }
                _tabButtons[i].AddThemeStyleboxOverride("normal", style);
                _tabButtons[i].AddThemeColorOverride("font_color", color);
            }

            // Populate entry list
            PopulateEntryList(_tabEntries[tabIndex]);
        }

        private void PopulateEntryList(EncyclopediaEntry[] entries)
        {
            // Clear existing entries
            foreach (var child in _entryList.GetChildren())
            {
                if (child is Node n)
                    n.QueueFree();
            }

            // Clear detail view
            _detailTitle.Text = "";
            _detailDescription.Text = "Select an entry from the list.";
            _detailStats.Text = "";

            // Add entry buttons
            for (int i = 0; i < entries.Length; i++)
            {
                int idx = i;
                var entry = entries[i];

                var btn = new Button();
                btn.Name = $"Entry_{entry.Id ?? i.ToString()}";
                btn.Text = entry.Name ?? entry.Id ?? $"Entry {i}";
                btn.AddThemeFontSizeOverride("font_size", 14);
                btn.CustomMinimumSize = new Vector2(0, 32);
                btn.Alignment = HorizontalAlignment.Left;

                var normalStyle = new StyleBoxFlat();
                normalStyle.BgColor = new Color(0.12f, 0.1f, 0.16f, 0.5f);
                normalStyle.SetCornerRadiusAll(2);
                btn.AddThemeStyleboxOverride("normal", normalStyle);

                var hoverStyle = new StyleBoxFlat();
                hoverStyle.BgColor = new Color(0.2f, 0.18f, 0.25f);
                hoverStyle.SetCornerRadiusAll(2);
                btn.AddThemeStyleboxOverride("hover", hoverStyle);

                btn.Pressed += () => ShowEntry(entry);
                _entryList.AddChild(btn);
            }
        }

        private void ShowEntry(EncyclopediaEntry entry)
        {
            _detailTitle.Text = entry.Name ?? entry.Id ?? "Unknown";
            _detailDescription.Text = entry.Description ?? "(No description available)";

            if (entry.Stats != null && entry.Stats.Count > 0)
            {
                string statsText = "--- Stats ---\n";
                foreach (var kvp in entry.Stats)
                {
                    statsText += $"  {kvp.Key}: {kvp.Value}\n";
                }
                _detailStats.Text = statsText;
            }
            else
            {
                _detailStats.Text = "";
            }
        }
    }
}
