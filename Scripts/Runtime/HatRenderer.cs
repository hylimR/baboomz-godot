using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders equipped hats above player heads. Each HatType (1-11) maps
    /// to a distinct colored shape matching the simulation enum:
    ///   1 TopHat, 2 AviatorCap, 3 Crown, 4 PirateHat, 5 ChefHat,
    ///   6 VikingHelmet, 7 WizardHat, 8 SamuraiHelmet, 9 DragonCrown,
    ///   10 Halo, 11 GoldenCrown.
    /// Follows player position with Y offset.
    /// </summary>
    public partial class HatRenderer : Node2D
    {
        private GameState _state;

        // Indexed by HatType enum value — must stay aligned with
        // Baboomz.Simulation.HatType in GameStateEnums.cs.
        private static readonly Color[] HatColors =
        {
            Colors.Transparent,           //  0 = None
            new Color(0.1f, 0.1f, 0.1f),  //  1 = TopHat (black)
            new Color(0.5f, 0.35f, 0.15f),//  2 = AviatorCap (brown leather)
            new Color(1f, 0.85f, 0.2f),   //  3 = Crown (gold)
            new Color(0.15f, 0.15f, 0.2f),//  4 = PirateHat (dark navy)
            new Color(0.95f, 0.95f, 0.95f),// 5 = ChefHat (white)
            new Color(0.6f, 0.2f, 0.2f),  //  6 = VikingHelmet (red-brown)
            new Color(0.3f, 0.3f, 0.8f),  //  7 = WizardHat (blue-purple)
            new Color(0.8f, 0.7f, 0.1f),  //  8 = SamuraiHelmet (gold)
            new Color(0.2f, 0.7f, 0.3f),  //  9 = DragonCrown (green)
            new Color(1f, 0.95f, 0.3f),   // 10 = Halo (pale gold)
            new Color(1f, 0.85f, 0f),     // 11 = GoldenCrown (bright gold)
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
                    case 1: // TopHat — tall cylinder with flat brim
                        DrawRect(new Rect2(pos + new Vector2(-4f, -9f), new Vector2(8f, 9f)), color);
                        DrawRect(new Rect2(pos + new Vector2(-7f, -1f), new Vector2(14f, 2f)), color);
                        break;
                    case 2: // AviatorCap — rounded dome with goggle band
                        DrawCircle(pos + new Vector2(0f, -3f), 5f, color);
                        DrawRect(new Rect2(pos + new Vector2(-5f, -3f), new Vector2(10f, 2f)),
                            new Color(0.2f, 0.2f, 0.2f));
                        break;
                    case 3: // Crown — three-point royal crown
                        DrawColoredPolygon(new[] {
                            pos + new Vector2(-6f, 0f),
                            pos + new Vector2(-6f, -3f),
                            pos + new Vector2(-4f, -6f),
                            pos + new Vector2(-2f, -3f),
                            pos + new Vector2(0f, -7f),
                            pos + new Vector2(2f, -3f),
                            pos + new Vector2(4f, -6f),
                            pos + new Vector2(6f, -3f),
                            pos + new Vector2(6f, 0f)
                        }, color);
                        break;
                    case 4: // PirateHat — bicorn with two peaks
                        DrawColoredPolygon(new[] {
                            pos + new Vector2(-8f, 0f),
                            pos + new Vector2(-6f, -5f),
                            pos + new Vector2(0f, -3f),
                            pos + new Vector2(6f, -5f),
                            pos + new Vector2(8f, 0f)
                        }, color);
                        break;
                    case 5: // ChefHat — poofy white cylinder
                        DrawCircle(pos + new Vector2(0f, -7f), 5f, color);
                        DrawRect(new Rect2(pos + new Vector2(-4f, -3f), new Vector2(8f, 3f)), color);
                        break;
                    case 6: // VikingHelmet — dome with triangle horns
                        DrawColoredPolygon(new[] {
                            pos + new Vector2(-5f, 0f),
                            pos + new Vector2(0f, -6f),
                            pos + new Vector2(5f, 0f)
                        }, color);
                        DrawLine(pos + new Vector2(-6f, -2f), pos + new Vector2(-8f, -8f), color, 1.5f);
                        DrawLine(pos + new Vector2(6f, -2f), pos + new Vector2(8f, -8f), color, 1.5f);
                        break;
                    case 7: // WizardHat — tall cone
                        DrawColoredPolygon(new[] {
                            pos + new Vector2(-5f, 0f),
                            pos + new Vector2(0f, -12f),
                            pos + new Vector2(5f, 0f)
                        }, color);
                        break;
                    case 8: // SamuraiHelmet — flat top with wings
                        DrawRect(new Rect2(pos + new Vector2(-6f, -3f), new Vector2(12f, 3f)), color);
                        DrawLine(pos + new Vector2(-6f, -3f), pos + new Vector2(-9f, -6f), color, 1.5f);
                        DrawLine(pos + new Vector2(6f, -3f), pos + new Vector2(9f, -6f), color, 1.5f);
                        break;
                    case 9: // DragonCrown — jagged top
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
                    case 10: // Halo — thin ring floating above head
                        DrawArc(pos + new Vector2(0f, -8f), 5f, 0f, Mathf.Tau, 24, color, 1.5f);
                        break;
                    case 11: // GoldenCrown — rounded with jewel
                        DrawRect(new Rect2(pos + new Vector2(-5f, -4f), new Vector2(10f, 4f)), color);
                        DrawCircle(pos + new Vector2(0f, -5f), 2f, new Color(1f, 0.2f, 0.2f));
                        break;
                }
            }
        }
    }
}
