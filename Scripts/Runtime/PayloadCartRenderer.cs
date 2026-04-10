using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders the Payload mode cart and its rail in world space:
    ///   - dashed line from GoalLeftX to GoalRightX showing the rail path
    ///   - colored cart sprite at the cart's current position
    ///   - tint changes based on push direction / contested / stopped
    /// Spawns nothing for non-Payload matches.
    /// </summary>
    public partial class PayloadCartRenderer : Node2D
    {
        private const float CartHalfWidth = 1.4f;
        private const float CartHeight = 1.6f;
        private const float RailY = 0f;            // visualised at world Y=0 (terrain top guess)
        private const float MovingThreshold = 0.05f;

        private GameState _state;
        private bool _active;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50;
            ZIndex = 11;
            _active = state?.Config != null
                      && state.Config.MatchType == MatchType.Payload;
            Visible = _active;
        }

        public override void _Process(double delta)
        {
            if (!_active) return;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (!_active || _state == null) return;

            ref var payload = ref _state.Payload;
            Vector2 cart = payload.Position.ToGodot();
            float railY = cart.Y; // follow the cart vertically so it always sits on the rail

            // Rail: dashed horizontal line spanning the goals.
            DrawDashedRail(payload.GoalLeftX, payload.GoalRightX, railY);

            // Goal markers (vertical bars at each end).
            var leftGoalColor = new Color(1f, 0.30f, 0.30f, 0.8f);  // P2's goal
            var rightGoalColor = new Color(0.30f, 0.55f, 1f, 0.8f); // P1's goal
            DrawGoal(payload.GoalLeftX, railY, leftGoalColor);
            DrawGoal(payload.GoalRightX, railY, rightGoalColor);

            // Cart body. Color encodes movement state:
            //   pushing right (towards P1's goal) -> blue
            //   pushing left  (towards P2's goal) -> red
            //   stopped / contested -> yellow
            Color cartColor;
            if (payload.VelocityX > MovingThreshold)
                cartColor = new Color(0.30f, 0.55f, 1.00f);
            else if (payload.VelocityX < -MovingThreshold)
                cartColor = new Color(1.00f, 0.30f, 0.30f);
            else
                cartColor = new Color(1.00f, 0.85f, 0.20f);

            // Pulse the cart when contested / stalemated.
            if (Mathf.Abs(payload.VelocityX) <= MovingThreshold && payload.StalemateTimer > 0f)
            {
                float p = 0.5f + 0.5f * Mathf.Sin(_state.Time * 6f);
                cartColor = cartColor.Lerp(Colors.White, p * 0.4f);
            }

            DrawCartBody(cart, cartColor);
        }

        private void DrawDashedRail(float leftX, float rightX, float y)
        {
            const float Dash = 1.0f;
            const float Gap = 0.6f;
            float minX = Mathf.Min(leftX, rightX);
            float maxX = Mathf.Max(leftX, rightX);
            // Godot Y-down: convert sim Y(=0) -> screen Y(=0).
            float godotY = -y;
            var color = new Color(0.85f, 0.75f, 0.45f, 0.7f);
            for (float x = minX; x < maxX; x += Dash + Gap)
            {
                float endX = Mathf.Min(x + Dash, maxX);
                DrawLine(new Vector2(x, godotY), new Vector2(endX, godotY), color, 0.18f);
            }
        }

        private void DrawGoal(float x, float y, Color color)
        {
            float top = -y - 2.0f;
            float bottom = -y + 0.3f;
            DrawLine(new Vector2(x, top), new Vector2(x, bottom), color, 0.25f);
            // Small triangle marker on top.
            var tri = new Vector2[]
            {
                new Vector2(x - 0.4f, top),
                new Vector2(x + 0.4f, top),
                new Vector2(x, top - 0.6f),
            };
            DrawColoredPolygon(tri, color);
        }

        private void DrawCartBody(Vector2 center, Color color)
        {
            // Wheels (two darker discs at the bottom of the body)
            var wheelColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            float wheelY = center.Y + CartHeight * 0.45f;
            DrawCircle(new Vector2(center.X - CartHalfWidth * 0.65f, wheelY), 0.35f, wheelColor);
            DrawCircle(new Vector2(center.X + CartHalfWidth * 0.65f, wheelY), 0.35f, wheelColor);

            // Body rect (above the wheels). Godot Y-down.
            var rect = new Rect2(
                new Vector2(center.X - CartHalfWidth, center.Y - CartHeight),
                new Vector2(CartHalfWidth * 2f, CartHeight));
            DrawRect(rect, color);

            // Brass band on top
            var top = new Rect2(
                new Vector2(center.X - CartHalfWidth, center.Y - CartHeight - 0.25f),
                new Vector2(CartHalfWidth * 2f, 0.25f));
            DrawRect(top, new Color(0.85f, 0.65f, 0.15f, 1f));

            // Outline
            var outline = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            DrawRect(rect, outline, false, 0.08f);
        }
    }
}
