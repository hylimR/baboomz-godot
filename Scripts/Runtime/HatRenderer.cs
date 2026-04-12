using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders equipped hats above player heads. Each HatType (1-5) maps
    /// to a distinct colored shape. Follows player position with Y offset.
    /// </summary>
    public partial class HatRenderer : Node2D
    {
        private GameState _state;
        private static readonly Color[] HatColors =
        {
            Colors.Transparent, // 0 = None
            new Color(0.6f, 0.2f, 0.2f), // 1 = Viking (red-brown)
            new Color(0.3f, 0.3f, 0.8f), // 2 = Wizard (blue-purple)
            new Color(0.8f, 0.7f, 0.1f), // 3 = Samurai (gold)
            new Color(0.2f, 0.7f, 0.3f), // 4 = Dragon Crown (green)
            new Color(1f, 0.85f, 0f),     // 5 = Golden Crown (bright gold)
        };

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 52;
            ZIndex = 20; // above player sprites
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_state == null) return;

            for (int i = 0; i < _state.Players.Length; i++)
            {
                ref PlayerState p = ref _state.Players[i];
                if (p.IsDead) continue;

                int hat = (int)p.Hat;
                if (hat <= 0 || hat >= HatColors.Length) continue;

                Vector2 pos = p.Position.ToGodot() + new Vector2(0f, -18f);
                Color color = HatColors[hat];

                switch (hat)
                {
                    case 1: // Viking helmet — triangle horns
                        DrawColoredPolygon(new[] {
                            pos + new Vector2(-5f, 0f),
                            pos + new Vector2(0f, -6f),
                            pos + new Vector2(5f, 0f)
                        }, color);
                        DrawLine(pos + new Vector2(-6f, -2f), pos + new Vector2(-8f, -8f), color, 0.1f);
                        DrawLine(pos + new Vector2(6f, -2f), pos + new Vector2(8f, -8f), color, 0.1f);
                        break;
                    case 2: // Wizard hat — tall cone
                        DrawColoredPolygon(new[] {
                            pos + new Vector2(-5f, 0f),
                            pos + new Vector2(0f, -12f),
                            pos + new Vector2(5f, 0f)
                        }, color);
                        break;
                    case 3: // Samurai helmet — flat top with wings
                        DrawRect(new Rect2(pos + new Vector2(-6f, -3f), new Vector2(12f, 3f)), color);
                        DrawLine(pos + new Vector2(-6f, -3f), pos + new Vector2(-9f, -6f), color, 0.1f);
                        DrawLine(pos + new Vector2(6f, -3f), pos + new Vector2(9f, -6f), color, 0.1f);
                        break;
                    case 4: // Dragon Crown — jagged top
                        DrawColoredPolygon(new[] {
                            pos + new Vector2(-5f, 0f),
                            pos + new Vector2(-4f, -5f),
                            pos + new Vector2(-1f, -2f),
                            pos + new Vector2(0f, -7f),
                            pos + new Vector2(1f, -2f),
                            pos + new Vector2(4f, -5f),
                            pos + new Vector2(5f, 0f)
                        }, color);
                        break;
                    case 5: // Golden Crown — rounded with jewel
                        DrawRect(new Rect2(pos + new Vector2(-5f, -4f), new Vector2(10f, 4f)), color);
                        DrawCircle(pos + new Vector2(0f, -5f), 2f, new Color(1f, 0.2f, 0.2f));
                        break;
                }
            }
        }
    }
}
