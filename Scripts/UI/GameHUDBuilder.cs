using Godot;

namespace Baboomz
{
    /// <summary>
    /// All references created by GameHUDBuilder.Build().
    /// </summary>
    public struct GameHUDRefs
    {
        // Player 1 (top-left)
        public Label P1NameLabel;
        public ColorRect P1HpFill;
        public ColorRect P1HpBg;
        public Label P1HpText;
        public ColorRect P1EpFill;
        public ColorRect P1EpBg;
        public Label P1EpText;

        // Player 2 (top-right)
        public Label P2NameLabel;
        public ColorRect P2HpFill;
        public ColorRect P2HpBg;

        // Center
        public Label WindLabel;
        public Label TimerLabel;
        public Label MatchStateLabel;

        // Bottom bar
        public ColorRect[] WeaponSlots;
        public Label[] WeaponLabels;
        public ColorRect[] SkillSlotRects;
        public Label[] SkillSlotLabels;
        public Label[] SkillCooldownLabels;
        public Button FireButton;
    }

    /// <summary>
    /// Builds the entire in-game HUD hierarchy from code. Run once at startup.
    /// Godot port of the Unity GameHUDBuilder.
    /// </summary>
    public static class GameHUDBuilder
    {
        public const int VisibleWeaponSlots = 7;
        public const int TotalWeaponSlots = 22;

        public static GameHUDRefs Build(Control root)
        {
            // Root should fill the entire viewport
            UIBuilder.SetAnchors(root, Vector2.Zero, Vector2.One);

            var refs = new GameHUDRefs
            {
                WeaponSlots = new ColorRect[TotalWeaponSlots],
                WeaponLabels = new Label[TotalWeaponSlots],
                SkillSlotRects = new ColorRect[2],
                SkillSlotLabels = new Label[2],
                SkillCooldownLabels = new Label[2]
            };

            BuildTopLeftPanel(root, ref refs);
            BuildTopRightPanel(root, ref refs);
            BuildTopCenterPanel(root, ref refs);
            BuildMatchStateText(root, ref refs);
            BuildBottomBar(root, ref refs);

            return refs;
        }

        private static void BuildTopLeftPanel(Control parent, ref GameHUDRefs refs)
        {
            var panel = UIBuilder.CreatePanel("TopLeftPanel",
                new Color(0f, 0f, 0f, 0.3f), parent,
                new Vector2(0f, 0f), new Vector2(0.3f, 0.15f));

            // Portrait placeholder
            UIBuilder.CreatePanel("P1Portrait",
                new Color(0.4f, 0.3f, 0.2f), panel,
                new Vector2(0.02f, 0.1f), new Vector2(0.2f, 0.9f));

            // Player 1 name
            refs.P1NameLabel = UIBuilder.CreateLabel("Player 1", 18, Colors.White,
                panel, new Vector2(0.22f, 0.05f), new Vector2(0.98f, 0.3f));

            // HP bar
            var (hpFill, hpBg) = UIBuilder.CreateBar("HPBar",
                UIBuilder.GrassGreen, new Color(0.2f, 0.2f, 0.2f, 0.8f),
                panel, new Vector2(0.22f, 0.35f), new Vector2(0.85f, 0.6f));
            refs.P1HpFill = hpFill;
            refs.P1HpBg = hpBg;

            refs.P1HpText = UIBuilder.CreateLabel("100/100", 14, Colors.White,
                panel, new Vector2(0.86f, 0.35f), new Vector2(0.98f, 0.6f));

            // Energy bar
            var (epFill, epBg) = UIBuilder.CreateBar("EnergyBar",
                UIBuilder.EnergyBlue, new Color(0.2f, 0.2f, 0.2f, 0.8f),
                panel, new Vector2(0.22f, 0.65f), new Vector2(0.85f, 0.9f));
            refs.P1EpFill = epFill;
            refs.P1EpBg = epBg;

            refs.P1EpText = UIBuilder.CreateLabel("", 14, Colors.White,
                panel, new Vector2(0.86f, 0.65f), new Vector2(0.98f, 0.9f));
        }

        private static void BuildTopRightPanel(Control parent, ref GameHUDRefs refs)
        {
            var panel = UIBuilder.CreatePanel("TopRightPanel",
                new Color(0f, 0f, 0f, 0.3f), parent,
                new Vector2(0.7f, 0f), new Vector2(1f, 0.15f));

            // Portrait placeholder
            UIBuilder.CreatePanel("P2Portrait",
                new Color(0.3f, 0.2f, 0.4f), panel,
                new Vector2(0.8f, 0.1f), new Vector2(0.98f, 0.9f));

            // Player 2 name
            refs.P2NameLabel = UIBuilder.CreateLabel("Player 2", 18, Colors.White,
                panel, new Vector2(0.02f, 0.05f), new Vector2(0.78f, 0.3f),
                HorizontalAlignment.Right);

            // HP bar
            var (hpFill, hpBg) = UIBuilder.CreateBar("P2HPBar",
                UIBuilder.GrassGreen, new Color(0.2f, 0.2f, 0.2f, 0.8f),
                panel, new Vector2(0.15f, 0.35f), new Vector2(0.78f, 0.6f));
            refs.P2HpFill = hpFill;
            refs.P2HpBg = hpBg;
        }

        private static void BuildTopCenterPanel(Control parent, ref GameHUDRefs refs)
        {
            refs.WindLabel = UIBuilder.CreateLabel("Wind: -- 0.0", 20, Colors.White,
                parent, new Vector2(0.35f, 0.02f), new Vector2(0.5f, 0.08f),
                HorizontalAlignment.Center);

            refs.TimerLabel = UIBuilder.CreateLabel("OK", 20, Colors.White,
                parent, new Vector2(0.5f, 0.02f), new Vector2(0.65f, 0.08f),
                HorizontalAlignment.Center);
        }

        private static void BuildMatchStateText(Control parent, ref GameHUDRefs refs)
        {
            refs.MatchStateLabel = UIBuilder.CreateLabel("", 28, Colors.White,
                parent, new Vector2(0.3f, 0.45f), new Vector2(0.7f, 0.55f),
                HorizontalAlignment.Center);
        }

        private static void BuildBottomBar(Control parent, ref GameHUDRefs refs)
        {
            // Bottom bar: fixed height 100px at screen bottom
            var bar = UIBuilder.CreatePanel("BottomBar",
                new Color(0.17f, 0.11f, 0.05f, 0.85f), parent,
                new Vector2(0f, 1f), new Vector2(1f, 1f));
            // Override to fixed height from the bottom
            bar.AnchorTop = 1f;
            bar.AnchorBottom = 1f;
            bar.OffsetTop = -100f;
            bar.OffsetBottom = 0f;

            // Weapon slots: 22 total, 7 visible at once, centered in bar
            float slotSize = 50f;
            float slotSpacing = 60f;

            for (int i = 0; i < TotalWeaponSlots; i++)
            {
                // Use absolute pixel positioning via offsets, centered in bar
                var slot = new ColorRect();
                slot.Name = $"WeaponSlot{i}";
                slot.Color = UIBuilder.Inactive;
                slot.MouseFilter = Control.MouseFilterEnum.Ignore;

                // Center anchor
                slot.AnchorLeft = 0.5f;
                slot.AnchorTop = 0.5f;
                slot.AnchorRight = 0.5f;
                slot.AnchorBottom = 0.5f;

                float xPos = (i - (VisibleWeaponSlots - 1) / 2f) * slotSpacing;
                slot.OffsetLeft = xPos - slotSize / 2f;
                slot.OffsetTop = -slotSize / 2f;
                slot.OffsetRight = xPos + slotSize / 2f;
                slot.OffsetBottom = slotSize / 2f;

                bar.AddChild(slot);
                refs.WeaponSlots[i] = slot;

                // Ammo/name label below each slot
                var label = new Label();
                label.Name = $"WeaponLabel{i}";
                label.Text = "";
                label.AddThemeFontSizeOverride("font_size", 9);
                label.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f, 0.8f));
                label.HorizontalAlignment = HorizontalAlignment.Center;

                label.AnchorLeft = 0.5f;
                label.AnchorTop = 0.5f;
                label.AnchorRight = 0.5f;
                label.AnchorBottom = 0.5f;
                label.OffsetLeft = xPos - slotSize / 2f;
                label.OffsetTop = slotSize / 2f;
                label.OffsetRight = xPos + slotSize / 2f;
                label.OffsetBottom = slotSize / 2f + 14f;

                bar.AddChild(label);
                refs.WeaponLabels[i] = label;

                // Initially hide slots beyond visible window
                slot.Visible = i < VisibleWeaponSlots;
                label.Visible = i < VisibleWeaponSlots;
            }

            // Skill slots (Q and E) to the left of weapon slots
            string[] skillKeys = { "Q", "E" };
            float skillStartX = -((VisibleWeaponSlots - 1) / 2f + 2.5f) * slotSpacing;
            for (int i = 0; i < 2; i++)
            {
                var skillRect = new ColorRect();
                skillRect.Name = $"SkillSlot{skillKeys[i]}";
                skillRect.Color = new Color(0.3f, 0.5f, 0.7f, 0.6f);
                skillRect.MouseFilter = Control.MouseFilterEnum.Ignore;

                float sx = skillStartX + i * slotSpacing;
                skillRect.AnchorLeft = 0.5f;
                skillRect.AnchorTop = 0.5f;
                skillRect.AnchorRight = 0.5f;
                skillRect.AnchorBottom = 0.5f;
                skillRect.OffsetLeft = sx - slotSize / 2f;
                skillRect.OffsetTop = -slotSize / 2f;
                skillRect.OffsetRight = sx + slotSize / 2f;
                skillRect.OffsetBottom = slotSize / 2f;

                bar.AddChild(skillRect);
                refs.SkillSlotRects[i] = skillRect;

                // Key hint label
                UIBuilder.CreateLabel(skillKeys[i], 14, Colors.White, skillRect,
                    new Vector2(0f, 0f), new Vector2(1f, 0.3f),
                    HorizontalAlignment.Center);

                // Skill name label
                var nameLabel = new Label();
                nameLabel.Name = $"SkillName{i}";
                nameLabel.Text = "";
                nameLabel.AddThemeFontSizeOverride("font_size", 9);
                nameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f, 0.8f));
                nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
                UIBuilder.SetAnchors(nameLabel, new Vector2(0f, 0.7f), new Vector2(1f, 1f));
                skillRect.AddChild(nameLabel);
                refs.SkillSlotLabels[i] = nameLabel;

                // Cooldown label (centered)
                var cdLabel = new Label();
                cdLabel.Name = $"SkillCD{i}";
                cdLabel.Text = "";
                cdLabel.AddThemeFontSizeOverride("font_size", 16);
                cdLabel.AddThemeColorOverride("font_color", Colors.White);
                cdLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.8f));
                cdLabel.AddThemeConstantOverride("outline_size", 2);
                cdLabel.HorizontalAlignment = HorizontalAlignment.Center;
                cdLabel.VerticalAlignment = VerticalAlignment.Center;
                UIBuilder.SetAnchors(cdLabel, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));
                skillRect.AddChild(cdLabel);
                refs.SkillCooldownLabels[i] = cdLabel;
            }

            // Fire button (right side of bottom bar)
            var fireBtn = UIBuilder.CreateButton("FireButton", "FIRE", 18,
                UIBuilder.HpRed, bar);
            fireBtn.AnchorLeft = 1f;
            fireBtn.AnchorTop = 0.5f;
            fireBtn.AnchorRight = 1f;
            fireBtn.AnchorBottom = 0.5f;
            fireBtn.OffsetLeft = -110f;
            fireBtn.OffsetTop = -35f;
            fireBtn.OffsetRight = -20f;
            fireBtn.OffsetBottom = 35f;
            refs.FireButton = fireBtn;
        }
    }
}
