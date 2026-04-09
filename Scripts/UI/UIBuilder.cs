using Godot;

namespace Baboomz
{
    /// <summary>
    /// Static utility with color constants and Control-node layout primitives.
    /// Godot port of the Unity UIBuilder. All HUD elements are built from code.
    /// </summary>
    public static class UIBuilder
    {
        // --- Color constants (from art palette) ---
        public static readonly Color HpRed = new Color(0.91f, 0.30f, 0.24f);       // #E74C3C
        public static readonly Color GrassGreen = new Color(0.48f, 0.75f, 0.26f);   // #7BC043
        public static readonly Color UiGold = new Color(0.945f, 0.769f, 0.059f);    // #F1C40F
        public static readonly Color EnergyBlue = new Color(0.204f, 0.596f, 0.859f);// #3498DB
        public static readonly Color Outline = new Color(0.169f, 0.114f, 0.055f);   // #2b1d0e
        public static readonly Color DarkBrown = new Color(0.17f, 0.11f, 0.05f);
        public static readonly Color SkyTop = new Color(0.157f, 0.333f, 0.627f);    // #2855A0
        public static readonly Color Inactive = new Color(0.7f, 0.7f, 0.7f);

        // --- Layout primitives ---

        /// <summary>
        /// Create a Label with font size, color, and outline.
        /// </summary>
        public static Label CreateLabel(string text, int fontSize, Color color, Control parent)
        {
            var label = new Label();
            label.Text = text;
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
            label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.8f));
            label.AddThemeConstantOverride("outline_size", 2);
            parent.AddChild(label);
            return label;
        }

        /// <summary>
        /// Create a Label with anchors (normalized 0-1 within parent).
        /// </summary>
        public static Label CreateLabel(string text, int fontSize, Color color,
            Control parent, Vector2 anchorMin, Vector2 anchorMax,
            HorizontalAlignment hAlign = HorizontalAlignment.Left)
        {
            var label = CreateLabel(text, fontSize, color, parent);
            label.HorizontalAlignment = hAlign;
            SetAnchors(label, anchorMin, anchorMax);
            return label;
        }

        /// <summary>
        /// Create a ColorRect panel with anchors and background color.
        /// </summary>
        public static ColorRect CreatePanel(string name, Color color, Control parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = new ColorRect();
            panel.Name = name;
            panel.Color = color;
            panel.MouseFilter = Control.MouseFilterEnum.Ignore;
            SetAnchors(panel, anchorMin, anchorMax);
            parent.AddChild(panel);
            return panel;
        }

        /// <summary>
        /// Create a ColorRect panel with fixed pixel size (centered at anchor).
        /// </summary>
        public static ColorRect CreatePanel(string name, Color color, Control parent)
        {
            var panel = new ColorRect();
            panel.Name = name;
            panel.Color = color;
            panel.MouseFilter = Control.MouseFilterEnum.Ignore;
            parent.AddChild(panel);
            return panel;
        }

        /// <summary>
        /// Create a health/energy bar using two overlapping ColorRects.
        /// Returns (fill, background).
        /// </summary>
        public static (ColorRect fill, ColorRect background) CreateBar(string name,
            Color fillColor, Color bgColor, Control parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            // Background
            var bg = new ColorRect();
            bg.Name = name;
            bg.Color = bgColor;
            bg.MouseFilter = Control.MouseFilterEnum.Ignore;
            SetAnchors(bg, anchorMin, anchorMax);
            parent.AddChild(bg);

            // Fill (child, full size by default)
            var fill = new ColorRect();
            fill.Name = "Fill";
            fill.Color = fillColor;
            fill.MouseFilter = Control.MouseFilterEnum.Ignore;
            fill.AnchorLeft = 0f;
            fill.AnchorTop = 0f;
            fill.AnchorRight = 1f;
            fill.AnchorBottom = 1f;
            fill.OffsetLeft = 0f;
            fill.OffsetTop = 0f;
            fill.OffsetRight = 0f;
            fill.OffsetBottom = 0f;
            bg.AddChild(fill);

            return (fill, bg);
        }

        /// <summary>
        /// Create a Button with text, font size, and background color.
        /// </summary>
        public static Button CreateButton(string name, string text, int fontSize,
            Color bgColor, Control parent)
        {
            var btn = new Button();
            btn.Name = name;
            btn.Text = text;
            btn.AddThemeFontSizeOverride("font_size", fontSize);

            var style = new StyleBoxFlat();
            style.BgColor = bgColor;
            style.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", style);

            var hoverStyle = new StyleBoxFlat();
            hoverStyle.BgColor = bgColor * 1.2f;
            hoverStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("hover", hoverStyle);

            var pressedStyle = new StyleBoxFlat();
            pressedStyle.BgColor = bgColor * 0.8f;
            pressedStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("pressed", pressedStyle);

            parent.AddChild(btn);
            return btn;
        }

        // --- Anchor utility ---

        /// <summary>
        /// Set anchors and zero offsets so the control fills the anchor region.
        /// anchorMin/anchorMax are normalized (0-1) within the parent.
        /// </summary>
        public static void SetAnchors(Control control, Vector2 anchorMin, Vector2 anchorMax)
        {
            control.AnchorLeft = anchorMin.X;
            control.AnchorTop = anchorMin.Y;
            control.AnchorRight = anchorMax.X;
            control.AnchorBottom = anchorMax.Y;
            control.OffsetLeft = 0f;
            control.OffsetTop = 0f;
            control.OffsetRight = 0f;
            control.OffsetBottom = 0f;
        }
    }
}
