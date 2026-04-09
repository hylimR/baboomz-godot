using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// 3, 2, 1, GO! countdown before match starts.
    /// Sets state to Waiting during countdown, then Playing when done.
    /// </summary>
    public partial class MatchCountdown : CanvasLayer
    {
        private GameState _state;
        private Label _countdownLabel;
        private float _timer = 3f;
        private bool _done;

        public void Init(GameState state)
        {
            _state = state;
            state.Phase = MatchPhase.Waiting;
            Layer = 20;

            _countdownLabel = new Label();
            _countdownLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _countdownLabel.VerticalAlignment = VerticalAlignment.Center;
            _countdownLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _countdownLabel.AddThemeFontSizeOverride("font_size", 80);
            _countdownLabel.AddThemeColorOverride("font_color", UIBuilder.UiGold);
            _countdownLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
            _countdownLabel.AddThemeConstantOverride("outline_size", 6);
            AddChild(_countdownLabel);
        }

        public override void _Process(double delta)
        {
            if (_done) return;

            _timer -= (float)delta;
            int display = (int)Mathf.Ceil(_timer);

            if (display > 0)
                _countdownLabel.Text = display.ToString();
            else if (display == 0)
                _countdownLabel.Text = "GO!";
            else
            {
                _state.Phase = MatchPhase.Playing;
                _done = true;
                QueueFree();
            }
        }
    }
}
