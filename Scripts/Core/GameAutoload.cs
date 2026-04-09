using Godot;

namespace Baboomz
{
    /// <summary>
    /// Autoload singleton — global state that persists across scenes.
    /// Replaces Unity's VContainer GameLifetimeScope + DontDestroyOnLoad.
    /// </summary>
    public partial class GameAutoload : Node
    {
        public static GameAutoload Instance { get; private set; }

        public GameSettings Settings { get; private set; }

        public override void _Ready()
        {
            Instance = this;
            Settings = new GameSettings();
            Settings.Load();
            GD.Print("GameAutoload ready");
        }
    }
}
