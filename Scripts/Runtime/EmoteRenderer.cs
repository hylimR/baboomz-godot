using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>
    /// Renders emote speech bubbles above players when EmoteEvents fire.
    /// Shows emote text for 2 seconds with fade-out. Positioned above hats.
    /// </summary>
    public partial class EmoteRenderer : Node2D
    {
        private GameState _state;
        private readonly List<EmoteBubble> _bubbles = new();

        private struct EmoteBubble
        {
            public int PlayerIndex;
            public string Text;
            public float Lifetime;
            public float MaxLifetime;
        }

        private const float BubbleDuration = 2f;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 53;
            ZIndex = 25; // above hats
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;
            float dt = (float)delta;

            // Spawn new bubbles from EmoteEvents
            foreach (var evt in _state.EmoteEvents)
            {
                string text = EmoteText.Get(evt.Emote);
                if (string.IsNullOrEmpty(text)) continue;

                // Replace existing bubble for same player
                bool replaced = false;
                for (int i = 0; i < _bubbles.Count; i++)
                {
                    if (_bubbles[i].PlayerIndex == evt.PlayerIndex)
                    {
                        _bubbles[i] = new EmoteBubble
                        {
                            PlayerIndex = evt.PlayerIndex,
                            Text = text,
                            Lifetime = BubbleDuration,
                            MaxLifetime = BubbleDuration
                        };
                        replaced = true;
                        break;
                    }
                }
                if (!replaced)
                {
                    _bubbles.Add(new EmoteBubble
                    {
                        PlayerIndex = evt.PlayerIndex,
                        Text = text,
                        Lifetime = BubbleDuration,
                        MaxLifetime = BubbleDuration
                    });
                }
            }

            // Update lifetimes
            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                var b = _bubbles[i];
                b.Lifetime -= dt;
                if (b.Lifetime <= 0f)
                {
                    _bubbles.RemoveAt(i);
                    continue;
                }
                _bubbles[i] = b;
            }

            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_state == null) return;
            var font = ThemeDB.FallbackFont;
            if (font == null) return;

            foreach (var bubble in _bubbles)
            {
                if (bubble.PlayerIndex < 0 || bubble.PlayerIndex >= _state.Players.Length)
                    continue;
                ref PlayerState p = ref _state.Players[bubble.PlayerIndex];
                if (p.IsDead) continue;

                Vector2 pos = p.Position.ToGodot() + new Vector2(0f, -35f);

                float alpha = bubble.Lifetime < 0.5f
                    ? bubble.Lifetime / 0.5f : 1f;

                // Speech bubble background
                var bgColor = new Color(0f, 0f, 0f, 0.6f * alpha);
                DrawRect(new Rect2(pos + new Vector2(-20f, -10f), new Vector2(40f, 16f)),
                    bgColor);

                // Emote text
                var textColor = new Color(1f, 1f, 1f, alpha);
                DrawString(font, pos + new Vector2(-16f, 2f), bubble.Text,
                    HorizontalAlignment.Center, 32, 12, textColor);
            }
        }

    }
}
