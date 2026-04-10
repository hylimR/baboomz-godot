using Godot;

namespace Baboomz
{
    /// <summary>
    /// Texture overlay helpers for the in-game HUD. Splits art-loading and
    /// frame-overlay logic out of GameHUDBuilder so that file stays under the
    /// 300/400-line caps.
    /// </summary>
    internal static class GameHUDArt
    {
        // Art paths (resolved through SpriteLoader; null = fall back to ColorRects).
        public const string PortraitFrame = "UI/Default/portrait_frame";
        public const string HpBarFrame    = "UI/Default/hp_bar_frame";
        public const string HpBarFill     = "UI/Default/hp_bar_fill";
        public const string EpBarFill     = "UI/Default/energy_bar_fill";
        public const string CompassDial   = "UI/Default/compass_dial";
        public const string CompassNeedle = "UI/Default/compass_needle";
        public const string BottomHudBar  = "UI/Default/bottom_hud_bar";
        public const string GearDecor     = "UI/Default/hud_gear_decoration";
        public const string WeaponSlot    = "UI/Default/weapon_slot";
        public const string FireButton    = "UI/Default/fire_button";
        public const string PanelBg       = "UI/Default/panel_bg";

        /// <summary>
        /// Adds a TextureRect frame on top of an existing Control. The frame fills
        /// the parent (anchors 0..1, optionally expanded outward). Returns null when
        /// the texture is missing so callers can fall back to placeholder rects.
        /// </summary>
        public static TextureRect AddFrameOverlay(Control parent, string artPath, Vector2 expand)
        {
            var tex = SpriteLoader.Load(artPath);
            if (tex == null) return null;
            var rect = new TextureRect();
            rect.Name = $"{System.IO.Path.GetFileName(artPath)}_frame";
            rect.Texture = tex;
            rect.StretchMode = TextureRect.StretchModeEnum.Scale;
            rect.MouseFilter = Control.MouseFilterEnum.Ignore;
            rect.AnchorLeft = 0f;
            rect.AnchorTop = 0f;
            rect.AnchorRight = 1f;
            rect.AnchorBottom = 1f;
            rect.OffsetLeft = -expand.X;
            rect.OffsetTop = -expand.Y;
            rect.OffsetRight = expand.X;
            rect.OffsetBottom = expand.Y;
            parent.AddChild(rect);
            return rect;
        }

        /// <summary>
        /// Adds a textured bar fill on top of a ColorRect. The ColorRect is set to
        /// opaque white so Modulate (set later by GameHUD) tints both the rect and
        /// the texture overlay together. The width-anchor logic in GameHUD.SetBarFill
        /// still drives how much of the bar is filled.
        /// </summary>
        public static void ApplyTexturedFill(ColorRect fill, string artPath)
        {
            var tex = SpriteLoader.Load(artPath);
            if (tex == null) return;
            fill.Color = Colors.White;
            fill.ClipContents = true;

            var overlay = new TextureRect();
            overlay.Name = "FillTex";
            overlay.Texture = tex;
            overlay.StretchMode = TextureRect.StretchModeEnum.Scale;
            overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
            overlay.AnchorLeft = 0f;
            overlay.AnchorTop = 0f;
            overlay.AnchorRight = 1f;
            overlay.AnchorBottom = 1f;
            fill.AddChild(overlay);
        }

        /// <summary>
        /// Builds the rotating wind compass (dial + needle) at the top-center of the
        /// HUD when the textures are present. Returns the needle TextureRect so HUD
        /// code can rotate it; returns null if the dial texture is missing.
        /// </summary>
        public static TextureRect BuildCompass(Control parent)
        {
            var dialTex = SpriteLoader.Load(CompassDial);
            if (dialTex == null) return null;

            var dial = new TextureRect();
            dial.Name = "CompassDial";
            dial.Texture = dialTex;
            dial.StretchMode = TextureRect.StretchModeEnum.Scale;
            dial.MouseFilter = Control.MouseFilterEnum.Ignore;
            dial.AnchorLeft = 0.5f; dial.AnchorRight = 0.5f;
            dial.AnchorTop = 0f;    dial.AnchorBottom = 0f;
            dial.OffsetLeft = -40f; dial.OffsetRight = 40f;
            dial.OffsetTop = 4f;    dial.OffsetBottom = 84f;
            parent.AddChild(dial);

            var needleTex = SpriteLoader.Load(CompassNeedle);
            if (needleTex == null) return null;

            var needle = new TextureRect();
            needle.Name = "CompassNeedle";
            needle.Texture = needleTex;
            needle.StretchMode = TextureRect.StretchModeEnum.Scale;
            needle.MouseFilter = Control.MouseFilterEnum.Ignore;
            needle.AnchorLeft = 0f; needle.AnchorTop = 0f;
            needle.AnchorRight = 1f; needle.AnchorBottom = 1f;
            needle.OffsetLeft = 12f; needle.OffsetTop = 12f;
            needle.OffsetRight = -12f; needle.OffsetBottom = -12f;
            needle.PivotOffset = new Vector2(28f, 28f);
            dial.AddChild(needle);
            return needle;
        }

        /// <summary>
        /// Adds the steampunk gear decoration to the bottom-left of the bar.
        /// </summary>
        public static void AddGearDecor(Control bar)
        {
            var gearTex = SpriteLoader.Load(GearDecor);
            if (gearTex == null) return;
            var gear = new TextureRect();
            gear.Name = "GearDecor";
            gear.Texture = gearTex;
            gear.StretchMode = TextureRect.StretchModeEnum.Scale;
            gear.MouseFilter = Control.MouseFilterEnum.Ignore;
            gear.AnchorLeft = 0f; gear.AnchorRight = 0f;
            gear.AnchorTop = 0f;  gear.AnchorBottom = 0f;
            gear.OffsetLeft = 8f; gear.OffsetTop = 8f;
            gear.OffsetRight = 88f; gear.OffsetBottom = 88f;
            bar.AddChild(gear);
        }

        /// <summary>
        /// Replaces the placeholder Button background with the steampunk fire art
        /// when the texture is present. Keeps the Button alive so click wiring
        /// (HUDBridge.GetFireButton) is unaffected.
        /// </summary>
        public static void ApplyFireButtonArt(Button fireBtn)
        {
            var fireTex = SpriteLoader.Load(FireButton);
            if (fireTex == null) return;

            var clearStyle = new StyleBoxFlat();
            clearStyle.BgColor = new Color(1f, 1f, 1f, 0f);
            fireBtn.AddThemeStyleboxOverride("normal", clearStyle);
            fireBtn.AddThemeStyleboxOverride("hover", clearStyle);
            fireBtn.AddThemeStyleboxOverride("pressed", clearStyle);
            fireBtn.Text = "";

            var fireArt = new TextureRect();
            fireArt.Name = "FireArt";
            fireArt.Texture = fireTex;
            fireArt.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            fireArt.MouseFilter = Control.MouseFilterEnum.Ignore;
            fireArt.AnchorLeft = 0f; fireArt.AnchorTop = 0f;
            fireArt.AnchorRight = 1f; fireArt.AnchorBottom = 1f;
            fireBtn.AddChild(fireArt);
        }
    }
}
