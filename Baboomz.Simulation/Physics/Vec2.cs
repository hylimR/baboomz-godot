using System;

namespace Baboomz.Simulation
{
    public struct Vec2 : IEquatable<Vec2>
    {
        public float x;
        public float y;

        public Vec2(float x, float y) { this.x = x; this.y = y; }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);
        public static readonly Vec2 One = new Vec2(1f, 1f);
        public static readonly Vec2 Up = new Vec2(0f, 1f);
        public static readonly Vec2 Down = new Vec2(0f, -1f);
        public static readonly Vec2 Left = new Vec2(-1f, 0f);
        public static readonly Vec2 Right = new Vec2(1f, 0f);

        public float Magnitude => MathF.Sqrt(x * x + y * y);
        public float SqrMagnitude => x * x + y * y;

        public Vec2 Normalized
        {
            get
            {
                float mag = Magnitude;
                return mag > 1e-6f ? new Vec2(x / mag, y / mag) : Zero;
            }
        }

        public static float Distance(Vec2 a, Vec2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        public static float Dot(Vec2 a, Vec2 b) => a.x * b.x + a.y * b.y;

        public static Vec2 Lerp(Vec2 a, Vec2 b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Vec2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.x * s, a.y * s);
        public static Vec2 operator *(float s, Vec2 a) => new Vec2(a.x * s, a.y * s);
        public static Vec2 operator /(Vec2 a, float s) => new Vec2(a.x / s, a.y / s);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.x, -a.y);

        public bool Equals(Vec2 other) => Math.Abs(x - other.x) < 1e-6f && Math.Abs(y - other.y) < 1e-6f;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(x, y);
        public static bool operator ==(Vec2 a, Vec2 b) => a.Equals(b);
        public static bool operator !=(Vec2 a, Vec2 b) => !a.Equals(b);

        public override string ToString() => $"({x:F2}, {y:F2})";
    }
}
