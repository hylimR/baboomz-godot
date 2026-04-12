using Godot;

namespace Baboomz
{
    /// <summary>
    /// Skill loadout selector for the main menu — cycle through 18 skills
    /// for Q and E slots, persist selection via GameSettings.
    /// </summary>
    public partial class MainMenuSetup
    {
        private void BuildSkillSelector(float y)
        {
            var config = new Simulation.GameConfig();
            int skillCount = config.Skills.Length;

            UIBuilder.CreateLabel("SKILLS", 16, new Color(0.8f, 0.8f, 0.8f),
                this, new Vector2(0.3f, y), new Vector2(0.7f, y + 0.03f),
                HorizontalAlignment.Center);
            y += 0.03f;

            // Skill slot Q
            var q = UIBuilder.CreateLabel("Q:", 14, new Color(0.7f, 0.8f, 1f),
                this, new Vector2(0.22f, y), new Vector2(0.27f, y + 0.04f),
                HorizontalAlignment.Right);
            q.VerticalAlignment = VerticalAlignment.Center;

            var leftQ = UIBuilder.CreateButton("SkillQ<", "<", 18,
                new Color(0.3f, 0.3f, 0.4f), this);
            UIBuilder.SetAnchors(leftQ, new Vector2(0.28f, y), new Vector2(0.32f, y + 0.04f));
            leftQ.Pressed += () => CycleSkill(0, -1);

            _skill0Label = UIBuilder.CreateLabel(GetSkillName(_skill0Index), 16,
                new Color(0.9f, 0.85f, 0.7f),
                this, new Vector2(0.32f, y), new Vector2(0.55f, y + 0.04f),
                HorizontalAlignment.Center);
            _skill0Label.VerticalAlignment = VerticalAlignment.Center;

            var rightQ = UIBuilder.CreateButton("SkillQ>", ">", 18,
                new Color(0.3f, 0.3f, 0.4f), this);
            UIBuilder.SetAnchors(rightQ, new Vector2(0.55f, y), new Vector2(0.59f, y + 0.04f));
            rightQ.Pressed += () => CycleSkill(0, 1);

            // Skill slot E
            y += 0.05f;
            var e = UIBuilder.CreateLabel("E:", 14, new Color(0.7f, 0.8f, 1f),
                this, new Vector2(0.22f, y), new Vector2(0.27f, y + 0.04f),
                HorizontalAlignment.Right);
            e.VerticalAlignment = VerticalAlignment.Center;

            var leftE = UIBuilder.CreateButton("SkillE<", "<", 18,
                new Color(0.3f, 0.3f, 0.4f), this);
            UIBuilder.SetAnchors(leftE, new Vector2(0.28f, y), new Vector2(0.32f, y + 0.04f));
            leftE.Pressed += () => CycleSkill(1, -1);

            _skill1Label = UIBuilder.CreateLabel(GetSkillName(_skill1Index), 16,
                new Color(0.9f, 0.85f, 0.7f),
                this, new Vector2(0.32f, y), new Vector2(0.55f, y + 0.04f),
                HorizontalAlignment.Center);
            _skill1Label.VerticalAlignment = VerticalAlignment.Center;

            var rightE = UIBuilder.CreateButton("SkillE>", ">", 18,
                new Color(0.3f, 0.3f, 0.4f), this);
            UIBuilder.SetAnchors(rightE, new Vector2(0.55f, y), new Vector2(0.59f, y + 0.04f));
            rightE.Pressed += () => CycleSkill(1, 1);

            // Apply loaded loadout
            GameModeContext.SelectedSkillSlot0 = _skill0Index;
            GameModeContext.SelectedSkillSlot1 = _skill1Index;
        }

        private void CycleSkill(int slot, int direction)
        {
            var config = new Simulation.GameConfig();
            int count = config.Skills.Length;

            if (slot == 0)
            {
                _skill0Index = (_skill0Index + direction + count) % count;
                _skill0Label.Text = GetSkillName(_skill0Index);
                GameModeContext.SelectedSkillSlot0 = _skill0Index;
                _settings.SkillSlot0 = _skill0Index;
            }
            else
            {
                _skill1Index = (_skill1Index + direction + count) % count;
                _skill1Label.Text = GetSkillName(_skill1Index);
                GameModeContext.SelectedSkillSlot1 = _skill1Index;
                _settings.SkillSlot1 = _skill1Index;
            }
            _settings.Save();
        }

        private static string GetSkillName(int index)
        {
            var config = new Simulation.GameConfig();
            if (index < 0 || index >= config.Skills.Length) return "???";
            string id = config.Skills[index].SkillId;
            return id.Replace('_', ' ');
        }
    }
}
