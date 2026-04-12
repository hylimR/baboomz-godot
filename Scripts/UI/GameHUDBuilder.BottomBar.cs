using Godot;

namespace Baboomz
{
    /// <summary>
    /// Bottom bar section of the HUD: weapon slots, skill slots, fire button.
    /// </summary>
    public static partial class GameHUDBuilder
    {
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

            // Steampunk overlay across the whole bar (real art covers the placeholder).
            var barArt = SpriteLoader.Load(GameHUDArt.BottomHudBar);
            if (barArt != null)
            {
                // Fade the placeholder to nearly transparent so the overlay shows through.
                bar.Color = new Color(bar.Color.R, bar.Color.G, bar.Color.B, 0.05f);
                GameHUDArt.AddFrameOverlay(bar, GameHUDArt.BottomHudBar, Vector2.Zero);
            }

            // Gear decoration on the bottom-left corner of the bar.
            GameHUDArt.AddGearDecor(bar);

            BuildWeaponSlots(bar, ref refs);
            BuildSkillSlots(bar, ref refs);
            BuildFireButton(bar, ref refs);
        }

        private static void BuildWeaponSlots(Control bar, ref GameHUDRefs refs)
        {
            float slotSize = 50f;
            float slotSpacing = 60f;

            var weaponSlotTex = SpriteLoader.Load(GameHUDArt.WeaponSlot);

            for (int i = 0; i < TotalWeaponSlots; i++)
            {
                var slot = new ColorRect();
                slot.Name = $"WeaponSlot{i}";
                slot.Color = Colors.White;
                slot.MouseFilter = Control.MouseFilterEnum.Ignore;

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

                if (weaponSlotTex != null)
                {
                    var slotFrame = new TextureRect();
                    slotFrame.Name = "Frame";
                    slotFrame.Texture = weaponSlotTex;
                    slotFrame.StretchMode = TextureRect.StretchModeEnum.Scale;
                    slotFrame.MouseFilter = Control.MouseFilterEnum.Ignore;
                    slotFrame.AnchorLeft = 0f; slotFrame.AnchorTop = 0f;
                    slotFrame.AnchorRight = 1f; slotFrame.AnchorBottom = 1f;
                    slot.AddChild(slotFrame);
                }

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

                slot.Visible = i < VisibleWeaponSlots;
                label.Visible = i < VisibleWeaponSlots;
            }
        }

        private static void BuildSkillSlots(Control bar, ref GameHUDRefs refs)
        {
            float slotSize = 50f;
            float slotSpacing = 60f;
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

                UIBuilder.CreateLabel(skillKeys[i], 14, Colors.White, skillRect,
                    new Vector2(0f, 0f), new Vector2(1f, 0.3f),
                    HorizontalAlignment.Center);

                var nameLabel = new Label();
                nameLabel.Name = $"SkillName{i}";
                nameLabel.Text = "";
                nameLabel.AddThemeFontSizeOverride("font_size", 9);
                nameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f, 0.8f));
                nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
                UIBuilder.SetAnchors(nameLabel, new Vector2(0f, 0.7f), new Vector2(1f, 1f));
                skillRect.AddChild(nameLabel);
                refs.SkillSlotLabels[i] = nameLabel;

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
        }

        private static void BuildFireButton(Control bar, ref GameHUDRefs refs)
        {
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

            GameHUDArt.ApplyFireButtonArt(fireBtn);
        }
    }
}
