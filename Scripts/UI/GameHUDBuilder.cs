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
        public TextureRect CompassNeedle;  // rotates with wind angle

        // Bottom bar
        public ColorRect[] WeaponSlots;
        public Label[] WeaponLabels;
        public Label WeaponPaginationLabel;
        public ColorRect[] SkillSlotRects;
        public Label[] SkillSlotLabels;
        public Label[] SkillCooldownLabels;
        public Button FireButton;

        // Overlays
        public DamageDirectionOverlay DamageDirectionOverlay;
    }

    /// <summary>
    /// Builds the entire in-game HUD hierarchy from code. Run once at startup.
    /// Godot port of the Unity GameHUDBuilder.
    /// </summary>
    public static partial class GameHUDBuilder
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
            BuildDamageDirectionOverlay(root, ref refs);

            return refs;
        }

        private static void BuildTopLeftPanel(Control parent, ref GameHUDRefs refs)
        {
            var panel = UIBuilder.CreatePanel("TopLeftPanel",
                new Color(0f, 0f, 0f, 0.3f), parent,
                new Vector2(0f, 0f), new Vector2(0.3f, 0.15f));

            // Portrait placeholder + steampunk frame overlay.
            var portrait = UIBuilder.CreatePanel("P1Portrait",
                new Color(0.4f, 0.3f, 0.2f), panel,
                new Vector2(0.02f, 0.1f), new Vector2(0.2f, 0.9f));
            GameHUDArt.AddFrameOverlay(portrait, GameHUDArt.PortraitFrame, new Vector2(6f, 6f));

            // Player 1 name
            refs.P1NameLabel = UIBuilder.CreateLabel("Player 1", 18, Colors.White,
                panel, new Vector2(0.22f, 0.05f), new Vector2(0.98f, 0.3f));

            // HP bar (with frame + textured fill)
            var (hpFill, hpBg) = UIBuilder.CreateBar("HPBar",
                Colors.White, new Color(0.2f, 0.2f, 0.2f, 0.8f),
                panel, new Vector2(0.22f, 0.35f), new Vector2(0.85f, 0.6f));
            refs.P1HpFill = hpFill;
            refs.P1HpBg = hpBg;
            GameHUDArt.ApplyTexturedFill(hpFill, GameHUDArt.HpBarFill);
            GameHUDArt.AddFrameOverlay(hpBg, GameHUDArt.HpBarFrame, new Vector2(4f, 6f));

            refs.P1HpText = UIBuilder.CreateLabel("100/100", 14, Colors.White,
                panel, new Vector2(0.86f, 0.35f), new Vector2(0.98f, 0.6f));

            // Energy bar (with frame + textured fill). White base so Modulate
            // (set by HUDBridge / GameHUD) drives the final color.
            var (epFill, epBg) = UIBuilder.CreateBar("EnergyBar",
                UIBuilder.EnergyBlue, new Color(0.2f, 0.2f, 0.2f, 0.8f),
                panel, new Vector2(0.22f, 0.65f), new Vector2(0.85f, 0.9f));
            refs.P1EpFill = epFill;
            refs.P1EpBg = epBg;
            GameHUDArt.ApplyTexturedFill(epFill, GameHUDArt.EpBarFill);
            GameHUDArt.AddFrameOverlay(epBg, GameHUDArt.HpBarFrame, new Vector2(4f, 6f));

            refs.P1EpText = UIBuilder.CreateLabel("", 14, Colors.White,
                panel, new Vector2(0.86f, 0.65f), new Vector2(0.98f, 0.9f));
        }

        private static void BuildTopRightPanel(Control parent, ref GameHUDRefs refs)
        {
            var panel = UIBuilder.CreatePanel("TopRightPanel",
                new Color(0f, 0f, 0f, 0.3f), parent,
                new Vector2(0.7f, 0f), new Vector2(1f, 0.15f));

            // Portrait placeholder + frame
            var portrait = UIBuilder.CreatePanel("P2Portrait",
                new Color(0.3f, 0.2f, 0.4f), panel,
                new Vector2(0.8f, 0.1f), new Vector2(0.98f, 0.9f));
            GameHUDArt.AddFrameOverlay(portrait, GameHUDArt.PortraitFrame, new Vector2(6f, 6f));

            // Player 2 name
            refs.P2NameLabel = UIBuilder.CreateLabel("Player 2", 18, Colors.White,
                panel, new Vector2(0.02f, 0.05f), new Vector2(0.78f, 0.3f),
                HorizontalAlignment.Right);

            // HP bar (with frame + textured fill)
            var (hpFill, hpBg) = UIBuilder.CreateBar("P2HPBar",
                Colors.White, new Color(0.2f, 0.2f, 0.2f, 0.8f),
                panel, new Vector2(0.15f, 0.35f), new Vector2(0.78f, 0.6f));
            refs.P2HpFill = hpFill;
            refs.P2HpBg = hpBg;
            GameHUDArt.ApplyTexturedFill(hpFill, GameHUDArt.HpBarFill);
            GameHUDArt.AddFrameOverlay(hpBg, GameHUDArt.HpBarFrame, new Vector2(4f, 6f));
        }

        private static void BuildTopCenterPanel(Control parent, ref GameHUDRefs refs)
        {
            // Real art: compass dial + rotating needle. Returns null when missing.
            refs.CompassNeedle = GameHUDArt.BuildCompass(parent);

            // Fallback wind text label — visible only when there's no compass art.
            refs.WindLabel = UIBuilder.CreateLabel("", 14, Colors.White,
                parent, new Vector2(0.4f, 0.0f), new Vector2(0.5f, 0.04f),
                HorizontalAlignment.Center);
            if (refs.CompassNeedle == null)
                refs.WindLabel.Text = "Wind: -- 0.0";

            refs.TimerLabel = UIBuilder.CreateLabel("OK", 20, Colors.White,
                parent, new Vector2(0.5f, 0.09f), new Vector2(0.65f, 0.13f),
                HorizontalAlignment.Center);
        }

        private static void BuildMatchStateText(Control parent, ref GameHUDRefs refs)
        {
            refs.MatchStateLabel = UIBuilder.CreateLabel("", 28, Colors.White,
                parent, new Vector2(0.3f, 0.45f), new Vector2(0.7f, 0.55f),
                HorizontalAlignment.Center);
        }

        private static void BuildDamageDirectionOverlay(Control parent, ref GameHUDRefs refs)
        {
            var overlay = new DamageDirectionOverlay();
            overlay.Name = "DamageDirectionOverlay";
            overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
            UIBuilder.SetAnchors(overlay, Vector2.Zero, Vector2.One);
            parent.AddChild(overlay);
            refs.DamageDirectionOverlay = overlay;
        }
    }
}
