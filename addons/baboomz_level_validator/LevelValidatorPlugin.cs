#if TOOLS
using Godot;
using System.Text;
using Baboomz;

namespace Baboomz.Editor
{
    /// <summary>
    /// EditorPlugin that adds a "Validate Levels" button to the editor toolbar.
    /// Runs LevelValidator on every *.json file in res://Resources/Levels/
    /// and prints a pass/fail summary to the Output panel.
    /// </summary>
    [Tool]
    public partial class LevelValidatorPlugin : EditorPlugin
    {
        private const string LevelsResourcePath = "res://Resources/Levels";
        private Button _button;

        public override void _EnterTree()
        {
            _button = new Button
            {
                Text = "Validate Levels",
                TooltipText = "Run LevelValidator schema checks on every level JSON in Resources/Levels/",
            };
            _button.Pressed += OnValidatePressed;
            AddControlToContainer(CustomControlContainer.Toolbar, _button);
        }

        public override void _ExitTree()
        {
            if (_button != null)
            {
                RemoveControlFromContainer(CustomControlContainer.Toolbar, _button);
                _button.QueueFree();
                _button = null;
            }
        }

        private void OnValidatePressed()
        {
            string dir = ProjectSettings.GlobalizePath(LevelsResourcePath);
            var reports = LevelValidator.ValidateDirectory(dir);

            int totalErrors = 0;
            int totalWarnings = 0;
            var sb = new StringBuilder();
            sb.AppendLine($"--- LevelValidator: {reports.Count} file(s) in {dir} ---");

            foreach (var r in reports)
            {
                string fileName = System.IO.Path.GetFileName(r.FilePath);
                if (r.ErrorCount == 0 && r.WarningCount == 0)
                {
                    sb.AppendLine($"  OK       {fileName}");
                }
                else
                {
                    string tag = r.ErrorCount > 0 ? "ERRORS " : "WARN   ";
                    sb.AppendLine($"  {tag} {fileName}  ({r.ErrorCount} error(s), {r.WarningCount} warning(s))");
                    foreach (var issue in r.Issues)
                        sb.AppendLine($"           {issue}");
                }
                totalErrors += r.ErrorCount;
                totalWarnings += r.WarningCount;
            }

            sb.AppendLine($"--- Summary: {totalErrors} error(s), {totalWarnings} warning(s) across {reports.Count} file(s) ---");

            if (totalErrors > 0)
                GD.PushError(sb.ToString());
            else if (totalWarnings > 0)
                GD.PushWarning(sb.ToString());
            else
                GD.Print(sb.ToString());
        }
    }
}
#endif
