using Godot;
using System.Collections.Generic;

namespace Baboomz
{
    public partial class DamageDirectionOverlay : Control
    {
        private readonly List<HitIndicator> _indicators = new();
        private const float FadeDuration = 1.5f;
        private const float RingRadius = 60f;
        private const float ArcLength = 0.4f; // radians (~23 degrees)
        private const float ArcWidth = 4f;

        private struct HitIndicator
        {
            public float Angle;
            public float Alpha;
        }

        public void AddHit(float angleRadians)
        {
            _indicators.Add(new HitIndicator { Angle = angleRadians, Alpha = 1f });
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            if (_indicators.Count == 0) return;

            float dt = (float)delta;
            for (int i = _indicators.Count - 1; i >= 0; i--)
            {
                var ind = _indicators[i];
                ind.Alpha -= dt / FadeDuration;
                if (ind.Alpha <= 0f)
                {
                    _indicators.RemoveAt(i);
                }
                else
                {
                    _indicators[i] = ind;
                }
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_indicators.Count == 0) return;

            var center = Size / 2f;
            int segments = 8;

            foreach (var ind in _indicators)
            {
                var color = new Color(1f, 0.15f, 0.1f, ind.Alpha * 0.8f);
                float startAngle = ind.Angle - ArcLength / 2f;

                for (int i = 0; i < segments; i++)
                {
                    float t0 = startAngle + (ArcLength * i / segments);
                    float t1 = startAngle + (ArcLength * (i + 1) / segments);
                    var p0 = center + new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)) * RingRadius;
                    var p1 = center + new Vector2(Mathf.Cos(t1), Mathf.Sin(t1)) * RingRadius;
                    DrawLine(p0, p1, color, ArcWidth);
                }
            }
        }
    }
}
