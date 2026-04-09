using Godot;

namespace Baboomz
{
    /// <summary>
    /// Creates simple procedural textures for placeholder visuals.
    /// Replaces Unity's ProceduralSprites utility.
    /// </summary>
    public static class ProceduralSprites
    {
        private static ImageTexture _whitePixel;

        public static ImageTexture WhitePixel
        {
            get
            {
                if (_whitePixel == null)
                {
                    var img = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
                    img.SetPixel(0, 0, Colors.White);
                    _whitePixel = ImageTexture.CreateFromImage(img);
                }
                return _whitePixel;
            }
        }

        public static ImageTexture CreateColorRect(int width, int height, Color color)
        {
            var img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
            img.Fill(color);
            return ImageTexture.CreateFromImage(img);
        }

        public static ImageTexture CreateCircle(int diameter, Color color)
        {
            var img = Image.CreateEmpty(diameter, diameter, false, Image.Format.Rgba8);
            float center = diameter / 2f;
            float radius = center;
            for (int y = 0; y < diameter; y++)
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                if (dx * dx + dy * dy <= radius * radius)
                    img.SetPixel(x, y, color);
            }
            return ImageTexture.CreateFromImage(img);
        }
    }
}
