using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// In-game HUD built entirely from code. Provides public API for
    /// HUDBridge to update each frame from GameState.
    /// Construction delegated to GameHUDBuilder.
    /// </summary>
    public partial class GameHUD : Control
    {
        // Player 1 (top-left)
        private Label p1NameLabel;
        private ColorRect p1HpFill;
        private ColorRect p1HpBg;
        private Label p1HpText;
        private ColorRect p1EpFill;
        private ColorRect p1EpBg;
        private Label p1EpText;

        // Player 2 (top-right)
        private Label p2NameLabel;
        private ColorRect p2HpFill;
        private ColorRect p2HpBg;

        // Center top
        private Label windLabel;
        private Label timerLabel;
        private Label matchStateLabel;
        private TextureRect compassNeedle;

        // Bottom bar
        private ColorRect[] weaponSlots;
        private Label[] weaponLabels;
        private ColorRect[] skillSlotRects;
        private Label[] skillSlotLabels;
        private Label[] skillCooldownLabels;
        private Button fireButton;

        // Weapon viewport state
        private int activeWeaponIndex;
        private int viewOffset;
        private int totalWeapons;
        private const int VisibleWeaponSlots = 7;

        public void BuildUI()
        {
            var refs = GameHUDBuilder.Build(this);

            p1NameLabel = refs.P1NameLabel;
            p1HpFill = refs.P1HpFill;
            p1HpBg = refs.P1HpBg;
            p1HpText = refs.P1HpText;
            p1EpFill = refs.P1EpFill;
            p1EpBg = refs.P1EpBg;
            p1EpText = refs.P1EpText;

            p2NameLabel = refs.P2NameLabel;
            p2HpFill = refs.P2HpFill;
            p2HpBg = refs.P2HpBg;

            windLabel = refs.WindLabel;
            timerLabel = refs.TimerLabel;
            matchStateLabel = refs.MatchStateLabel;
            compassNeedle = refs.CompassNeedle;

            weaponSlots = refs.WeaponSlots;
            weaponLabels = refs.WeaponLabels;
            skillSlotRects = refs.SkillSlotRects;
            skillSlotLabels = refs.SkillSlotLabels;
            skillCooldownLabels = refs.SkillCooldownLabels;
            fireButton = refs.FireButton;
        }

        // --- Public API ---

        public void SetMatchState(string state)
        {
            if (matchStateLabel != null) matchStateLabel.Text = state;
        }

        public void SetP1Name(string name)
        {
            if (p1NameLabel != null) p1NameLabel.Text = name;
        }

        public void SetP2Name(string name)
        {
            if (p2NameLabel != null) p2NameLabel.Text = name;
        }

        public void SetHPFill(float normalised, float current = -1f, float max = -1f)
        {
            if (p1HpFill != null)
            {
                float clamped = Mathf.Clamp(normalised, 0f, 1f);
                SetBarFill(p1HpFill, p1HpBg, clamped);
                // Modulate propagates to the texture overlay child (when present);
                // when there's no overlay, the Color is preserved from the builder.
                p1HpFill.Modulate = UIBuilder.HpRed.Lerp(UIBuilder.GrassGreen, clamped);
            }
            if (p1HpText != null && current >= 0f)
                p1HpText.Text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        public void SetP2HPFill(float normalised)
        {
            if (p2HpFill != null)
            {
                float clamped = Mathf.Clamp(normalised, 0f, 1f);
                SetBarFill(p2HpFill, p2HpBg, clamped);
                p2HpFill.Modulate = UIBuilder.HpRed.Lerp(UIBuilder.GrassGreen, clamped);
            }
        }

        public void SetEPFill(float normalised, float current = -1f, float max = -1f)
        {
            if (p1EpFill != null)
            {
                float clamped = Mathf.Clamp(normalised, 0f, 1f);
                SetBarFill(p1EpFill, p1EpBg, clamped);
            }
            if (p1EpText != null && current >= 0f)
                p1EpText.Text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        public void SetWind(float angleDegrees, float strength)
        {
            // Compass needle (steampunk art): rotate around the dial center.
            if (compassNeedle != null)
            {
                compassNeedle.Rotation = Mathf.DegToRad(angleDegrees);
                // Tint stronger winds orange.
                float ts = Mathf.Clamp(strength / 3f, 0f, 1f);
                compassNeedle.Modulate = Colors.White.Lerp(new Color(1f, 0.6f, 0.2f), ts);
            }

            // Always update the text label too — it's hidden when the compass is
            // available, but kept around as a fallback for missing-art runs.
            if (windLabel == null) return;

            bool leftWind = angleDegrees >= 0f && angleDegrees < 180f;
            string arrow = leftWind ? "<<<" : ">>>";
            int bars = Mathf.Clamp(Mathf.RoundToInt(strength), 0, 5);
            string barStr = new string('|', bars);

            float t = Mathf.Clamp(strength / 3f, 0f, 1f);
            windLabel.AddThemeColorOverride("font_color",
                Colors.White.Lerp(new Color(1f, 0.6f, 0.2f), t));
            // Suppress the text indicator when the compass needle is rendering.
            windLabel.Text = compassNeedle != null ? "" : $"{arrow} {barStr}";
        }

        public void SetCooldownDisplay(float cooldownPercent)
        {
            if (timerLabel == null) return;
            if (cooldownPercent >= 1f)
            {
                timerLabel.Text = "OK";
                timerLabel.AddThemeColorOverride("font_color", Colors.White);
            }
            else
            {
                timerLabel.Text = ((1f - cooldownPercent) * 2f).ToString("F1");
                timerLabel.AddThemeColorOverride("font_color", UIBuilder.HpRed);
            }
        }

        public void SetTotalWeapons(int count) => totalWeapons = count;

        public void SelectWeapon(int index)
        {
            if (index < 0 || weaponSlots == null) return;
            activeWeaponIndex = index;

            // Scroll viewport if needed
            if (totalWeapons > VisibleWeaponSlots)
            {
                if (index < viewOffset) viewOffset = index;
                else if (index >= viewOffset + VisibleWeaponSlots)
                    viewOffset = index - (VisibleWeaponSlots - 1);
                viewOffset = System.Math.Max(0,
                    System.Math.Min(viewOffset, totalWeapons - VisibleWeaponSlots));
            }
            else
            {
                viewOffset = 0;
            }

            RefreshWeaponSlots();
        }

        public void SetWeaponSlotAmmo(int index, string label)
        {
            if (weaponLabels != null && index >= 0 && index < weaponLabels.Length
                && weaponLabels[index] != null)
                weaponLabels[index].Text = label;
        }

        public void SetSkillSlotName(int slot, string name)
        {
            if (slot >= 0 && slot < skillSlotLabels.Length && skillSlotLabels[slot] != null)
                skillSlotLabels[slot].Text = name;
        }

        public void SetSkillSlotCooldown(int slot, float cooldownNormalized, string text)
        {
            if (slot < 0 || slot >= 2) return;
            if (skillCooldownLabels[slot] != null)
                skillCooldownLabels[slot].Text = text;
            if (skillSlotRects[slot] != null)
            {
                float alpha = cooldownNormalized < 1f ? 0.3f : 0.6f;
                Color c = cooldownNormalized >= 1f
                    ? new Color(0.3f, 0.5f, 0.7f, alpha)
                    : new Color(0.3f, 0.3f, 0.3f, alpha);
                skillSlotRects[slot].Color = c;
            }
        }

        public Button GetFireButton() => fireButton;

        // --- Private helpers ---

        /// <summary>
        /// Adjusts fill rect width as a fraction of background width.
        /// </summary>
        private static void SetBarFill(ColorRect fill, ColorRect bg, float normalised)
        {
            if (fill == null || bg == null) return;
            // Fill is a child of bg with anchors 0-1. We set AnchorRight to normalised.
            fill.AnchorRight = normalised;
        }

        private void RefreshWeaponSlots()
        {
            if (weaponSlots == null) return;
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == null) continue;
                bool inWindow = i >= viewOffset && i < viewOffset + VisibleWeaponSlots;
                weaponSlots[i].Visible = inWindow;
                if (weaponLabels[i] != null)
                    weaponLabels[i].Visible = inWindow;

                Color tint = (i == activeWeaponIndex) ? UIBuilder.UiGold : UIBuilder.Inactive;
                tint.A = 1f;
                // Modulate propagates to the texture overlay child (when present);
                // when there is no overlay, the underlying ColorRect is also tinted
                // because Modulate multiplies its Color.
                weaponSlots[i].Modulate = tint;
            }
        }
    }
}
