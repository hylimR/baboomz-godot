using Godot;

namespace Baboomz
{
    /// <summary>
    /// ESC toggles pause overlay. Uses ProcessMode Always so it
    /// keeps receiving input while the tree is paused.
    /// </summary>
    public partial class PauseMenu : Control
    {
        private Control _overlay;
        private bool _isPaused;

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            Visible = false;

            BuildUI();
        }

        private void BuildUI()
        {
            // Full-screen semi-transparent overlay
            _overlay = new ColorRect();
            _overlay.Name = "Overlay";
            ((ColorRect)_overlay).Color = new Color(0f, 0f, 0f, 0.5f);
            UIBuilder.SetAnchors(_overlay, Vector2.Zero, Vector2.One);
            AddChild(_overlay);

            // Center panel
            var panel = UIBuilder.CreatePanel("CenterPanel",
                new Color(0.15f, 0.1f, 0.05f, 0.95f), _overlay,
                new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f));

            // "PAUSED" title
            var title = UIBuilder.CreateLabel("PAUSED", 42, Colors.White,
                panel, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.3f),
                HorizontalAlignment.Center);
            title.VerticalAlignment = VerticalAlignment.Center;

            // Resume button
            var resumeBtn = UIBuilder.CreateButton("ResumeBtn", "Resume", 24,
                new Color(0.3f, 0.5f, 0.3f), panel);
            UIBuilder.SetAnchors(resumeBtn, new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.58f));
            resumeBtn.Pressed += Resume;

            // Main Menu button
            var menuBtn = UIBuilder.CreateButton("MainMenuBtn", "Main Menu", 24,
                new Color(0.5f, 0.3f, 0.3f), panel);
            UIBuilder.SetAnchors(menuBtn, new Vector2(0.2f, 0.65f), new Vector2(0.8f, 0.83f));
            menuBtn.Pressed += QuitToMainMenu;

            _overlay.Visible = false;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
            {
                if (_isPaused)
                    Resume();
                else
                    Pause();

                GetViewport().SetInputAsHandled();
            }
        }

        public void Pause()
        {
            _isPaused = true;
            GetTree().Paused = true;
            _overlay.Visible = true;
            Visible = true;
        }

        public void Resume()
        {
            _isPaused = false;
            GetTree().Paused = false;
            _overlay.Visible = false;
            Visible = false;
        }

        private void QuitToMainMenu()
        {
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
        }
    }
}
