using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// HUD overlay that displays the current tutorial step, progress, and
    /// a completion message. Reads GameState.Tutorial each frame.
    /// Only visible when a tutorial is active.
    /// </summary>
    public partial class TutorialOverlay : Control
    {
        private GameState _state;
        private Label _stepLabel;
        private Label _descLabel;
        private Label _progressLabel;
        private Label _completeLabel;
        private ColorRect _panel;
        private int _lastStepIndex = -1;
        private float _stepFlashTimer;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 90;

            if (_state?.Tutorial == null)
            {
                Visible = false;
                return;
            }

            BuildUI();
            Visible = true;
            UpdateDisplay();
        }

        private void BuildUI()
        {
            // Semi-transparent panel at top of screen
            _panel = UIBuilder.CreatePanel("TutorialPanel",
                new Color(0.05f, 0.05f, 0.15f, 0.85f), this,
                new Vector2(0.2f, 0.01f), new Vector2(0.8f, 0.14f));
            _panel.MouseFilter = MouseFilterEnum.Ignore;

            // Step counter (e.g. "Step 3 / 9")
            _stepLabel = UIBuilder.CreateLabel("", 14,
                new Color(0.7f, 0.8f, 1f),
                _panel, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.3f),
                HorizontalAlignment.Center);

            // Step description
            _descLabel = UIBuilder.CreateLabel("", 18, UIBuilder.UiGold,
                _panel, new Vector2(0.03f, 0.3f), new Vector2(0.97f, 0.7f),
                HorizontalAlignment.Center);
            _descLabel.VerticalAlignment = VerticalAlignment.Center;

            // Progress hint
            _progressLabel = UIBuilder.CreateLabel("", 12,
                new Color(0.6f, 0.8f, 0.6f),
                _panel, new Vector2(0.03f, 0.7f), new Vector2(0.97f, 0.95f),
                HorizontalAlignment.Center);

            // Completion message (hidden until tutorial ends)
            _completeLabel = UIBuilder.CreateLabel("Tutorial Complete!", 28,
                UIBuilder.UiGold,
                this, new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.45f),
                HorizontalAlignment.Center);
            _completeLabel.VerticalAlignment = VerticalAlignment.Center;
            _completeLabel.Visible = false;
        }

        public override void _Process(double delta)
        {
            if (_state?.Tutorial == null) return;

            var tut = _state.Tutorial;

            if (tut.IsComplete)
            {
                _panel.Visible = false;
                _completeLabel.Visible = true;
                return;
            }

            if (tut.IsSkipped)
            {
                Visible = false;
                return;
            }

            // Flash on step advance
            if (tut.StepJustCompleted || tut.CurrentStepIndex != _lastStepIndex)
            {
                _stepFlashTimer = 0.5f;
                _lastStepIndex = tut.CurrentStepIndex;
                TutorialSystem.InitStepTracking(tut, _state);
            }

            if (_stepFlashTimer > 0f)
            {
                _stepFlashTimer -= (float)delta;
                float flash = _stepFlashTimer > 0f ? 0.3f : 0f;
                _panel.Color = new Color(0.1f + flash, 0.15f + flash, 0.2f + flash, 0.85f);
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var tut = _state.Tutorial;
            if (tut == null || tut.IsComplete || tut.IsSkipped) return;

            var step = TutorialSystem.GetCurrentStep(tut);
            if (step == null) return;

            int total = tut.Steps.Length;
            int current = tut.CurrentStepIndex + 1;

            _stepLabel.Text = $"Step {current} / {total} — {step.Title}";
            _descLabel.Text = step.Description;

            // Show progress for steps that have measurable thresholds
            if (step.Threshold > 1f)
            {
                float pct = Mathf.Clamp(tut.StepProgress / step.Threshold, 0f, 1f) * 100f;
                _progressLabel.Text = $"{pct:F0}%";
            }
            else
            {
                _progressLabel.Text = "";
            }
        }
    }
}
