using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>
    /// Scrolling event log — shows hits, kills, weapon names.
    /// Watches DamageEvents each frame and displays entries that fade out.
    /// </summary>
    public partial class KillFeed : CanvasLayer
    {
        private GameState _state;
        private VBoxContainer _container;
        private readonly List<(Label label, float timer)> _entries = new();
        private const float EntryLifetime = 4f;
        private const int MaxEntries = 5;
        private bool[] _wasAlive;

        public void Init(GameState state)
        {
            _state = state;
            Layer = 11;

            var margin = new MarginContainer();
            margin.SetAnchorsPreset(Control.LayoutPreset.TopRight);
            margin.AnchorLeft = 0.65f;
            margin.AnchorRight = 1f;
            margin.AnchorTop = 0.05f;
            margin.AnchorBottom = 0.4f;
            margin.OffsetLeft = 0f;
            margin.OffsetRight = -10f;
            margin.OffsetTop = 0f;
            margin.OffsetBottom = 0f;
            AddChild(margin);

            _container = new VBoxContainer();
            _container.Alignment = BoxContainer.AlignmentMode.Begin;
            margin.AddChild(_container);

            // Track alive state for death detection
            _wasAlive = new bool[state.Players.Length];
            for (int i = 0; i < state.Players.Length; i++)
                _wasAlive[i] = !state.Players[i].IsDead;

            ProcessPriority = 75;
        }

        public void AddEntry(string text, Color color)
        {
            var label = new Label();
            label.Text = text;
            label.HorizontalAlignment = HorizontalAlignment.Right;
            label.AddThemeFontSizeOverride("font_size", 14);
            label.AddThemeColorOverride("font_color", color);
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            label.AddThemeConstantOverride("outline_size", 2);
            _container.AddChild(label);
            _entries.Add((label, EntryLifetime));

            while (_entries.Count > MaxEntries)
            {
                _entries[0].label.QueueFree();
                _entries.RemoveAt(0);
            }
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            // Process damage events this tick
            foreach (var evt in _state.DamageEvents)
            {
                string attacker = evt.SourceIndex >= 0
                    ? _state.Players[evt.SourceIndex].Name
                    : "World";
                string target = _state.Players[evt.TargetIndex].Name;
                AddEntry($"{attacker} hit {target} ({evt.Amount:F0})", UIBuilder.UiGold);
            }

            // Detect new deaths
            for (int i = 0; i < _state.Players.Length; i++)
            {
                if (_wasAlive[i] && _state.Players[i].IsDead)
                {
                    string name = _state.Players[i].Name;
                    AddEntry($"{name} eliminated!", UIBuilder.HpRed);
                }
                _wasAlive[i] = !_state.Players[i].IsDead;
            }

            // Fade entries
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var (label, timer) = _entries[i];
                timer -= (float)delta;
                _entries[i] = (label, timer);
                label.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(timer / 1f, 0f, 1f));
                if (timer <= 0f)
                {
                    label.QueueFree();
                    _entries.RemoveAt(i);
                }
            }
        }
    }
}
