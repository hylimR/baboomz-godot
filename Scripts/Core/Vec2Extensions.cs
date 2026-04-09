using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Bridge between simulation Vec2 (Y-up) and Godot Vector2 (Y-down).
    /// </summary>
    public static class Vec2Extensions
    {
        public static Vector2 ToGodot(this Vec2 v) => new Vector2(v.x, -v.y);
        public static Vec2 ToSim(this Vector2 v) => new Vec2(v.X, -v.Y);
    }
}
