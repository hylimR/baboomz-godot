using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Reads Godot Input each frame and writes to GameState.PlayerInputs.
    /// Supports keyboard (player 0) + gamepad.
    /// </summary>
    public partial class InputBridge : Node
    {
        private GameState _state;

        // Previous frame state for edge detection
        private bool _prevFire;
        private bool _prevJump;
        private bool _prevSkill1;
        private bool _prevSkill2;
        private bool[] _prevWeaponSlots = new bool[10];
        private bool[] _prevEmotes = new bool[4];
        private bool _prevScrollUp;
        private bool _prevScrollDown;

        public void SetState(GameState state) => _state = state;

        public override void _Process(double delta)
        {
            if (_state == null) return;
            _state.PlayerInputs[0] = ReadKeyboard();

            // Gamepad support for player 0 (merge with keyboard)
            if (Input.GetConnectedJoypads().Count > 0)
            {
                var gp = ReadGamepad(0);
                _state.PlayerInputs[0] = Merge(_state.PlayerInputs[0], gp);
            }

            // Second gamepad for player 1
            if (Input.GetConnectedJoypads().Count >= 2
                && _state.Players.Length > 1 && !_state.Players[1].IsAI)
            {
                _state.PlayerInputs[1] = ReadGamepad(1);
            }
        }

        private InputState ReadKeyboard()
        {
            var input = new InputState();

            // Movement
            float left = Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left) ? -1f : 0f;
            float right = Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right) ? 1f : 0f;
            input.MoveX = left + right;

            // Jump (edge detect)
            bool jumpNow = Input.IsKeyPressed(Key.Space);
            input.JumpPressed = jumpNow && !_prevJump;
            _prevJump = jumpNow;

            // Aim
            float aimUp = Input.IsKeyPressed(Key.W) ? 1f : 0f;
            float aimDown = Input.IsKeyPressed(Key.S) ? -1f : 0f;
            input.AimDelta = aimUp + aimDown;

            // Fire (edge detect for pressed/released)
            bool fireNow = Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsKeyPressed(Key.F);
            input.FirePressed = fireNow && !_prevFire;
            input.FireHeld = fireNow;
            input.FireReleased = !fireNow && _prevFire;
            _prevFire = fireNow;

            // Weapon slots (1-0 keys, edge detect)
            input.WeaponSlotPressed = -1;
            Key[] slotKeys = { Key.Key1, Key.Key2, Key.Key3, Key.Key4, Key.Key5,
                               Key.Key6, Key.Key7, Key.Key8, Key.Key9, Key.Key0 };
            for (int i = 0; i < 10; i++)
            {
                bool pressed = Input.IsKeyPressed(slotKeys[i]);
                if (pressed && !_prevWeaponSlots[i])
                    input.WeaponSlotPressed = i;
                _prevWeaponSlots[i] = pressed;
            }

            // Weapon scroll ([ ] keys, edge detect)
            input.WeaponScrollDelta = 0;
            bool scrollUp = Input.IsKeyPressed(Key.Bracketright);
            bool scrollDown = Input.IsKeyPressed(Key.Bracketleft);
            if (scrollUp && !_prevScrollUp) input.WeaponScrollDelta = 1;
            if (scrollDown && !_prevScrollDown) input.WeaponScrollDelta = -1;
            _prevScrollUp = scrollUp;
            _prevScrollDown = scrollDown;

            // Skills (edge detect)
            bool skill1 = Input.IsKeyPressed(Key.Q);
            bool skill2 = Input.IsKeyPressed(Key.E);
            input.Skill1Pressed = skill1 && !_prevSkill1;
            input.Skill2Pressed = skill2 && !_prevSkill2;
            _prevSkill1 = skill1;
            _prevSkill2 = skill2;

            // Emotes (F5-F8, edge detect)
            input.EmotePressed = 0;
            Key[] emoteKeys = { Key.F5, Key.F6, Key.F7, Key.F8 };
            for (int i = 0; i < 4; i++)
            {
                bool pressed = Input.IsKeyPressed(emoteKeys[i]);
                if (pressed && !_prevEmotes[i])
                    input.EmotePressed = i + 1;
                _prevEmotes[i] = pressed;
            }

            return input;
        }

        private InputState ReadGamepad(int deviceIndex)
        {
            var input = new InputState();
            float deadzone = 0.15f;

            // Left stick X
            float leftX = Input.GetJoyAxis(deviceIndex, JoyAxis.LeftX);
            input.MoveX = Mathf.Abs(leftX) > deadzone ? leftX : 0f;

            // Right stick Y (aim)
            float rightY = Input.GetJoyAxis(deviceIndex, JoyAxis.RightY);
            input.AimDelta = Mathf.Abs(rightY) > deadzone ? -rightY * 1.5f : 0f; // Negate: stick up = aim up

            // A button = jump
            input.JumpPressed = Input.IsJoyButtonPressed(deviceIndex, JoyButton.A);

            // Right trigger = fire
            float rt = Input.GetJoyAxis(deviceIndex, JoyAxis.TriggerRight);
            input.FireHeld = rt > 0.5f;
            // Note: edge detection for gamepad would need per-device prev state
            // Simplified for now

            // Right bumper = weapon scroll
            input.WeaponScrollDelta = 0;
            if (Input.IsJoyButtonPressed(deviceIndex, JoyButton.RightShoulder))
                input.WeaponScrollDelta = 1;

            // Left bumper = skill 1, left trigger = skill 2
            input.Skill1Pressed = Input.IsJoyButtonPressed(deviceIndex, JoyButton.LeftShoulder);
            float lt = Input.GetJoyAxis(deviceIndex, JoyAxis.TriggerLeft);
            input.Skill2Pressed = lt > 0.5f;

            input.WeaponSlotPressed = -1;
            input.EmotePressed = 0;

            return input;
        }

        /// <summary>
        /// Merge keyboard + gamepad: larger magnitude for analog, OR for buttons.
        /// </summary>
        private static InputState Merge(InputState a, InputState b)
        {
            var result = new InputState();
            result.MoveX = Mathf.Abs(b.MoveX) > Mathf.Abs(a.MoveX) ? b.MoveX : a.MoveX;
            result.AimDelta = Mathf.Abs(b.AimDelta) > Mathf.Abs(a.AimDelta) ? b.AimDelta : a.AimDelta;
            result.JumpPressed = a.JumpPressed || b.JumpPressed;
            result.FirePressed = a.FirePressed || b.FirePressed;
            result.FireHeld = a.FireHeld || b.FireHeld;
            result.FireReleased = a.FireReleased || b.FireReleased;
            result.Skill1Pressed = a.Skill1Pressed || b.Skill1Pressed;
            result.Skill2Pressed = a.Skill2Pressed || b.Skill2Pressed;
            result.WeaponSlotPressed = b.WeaponSlotPressed >= 0 ? b.WeaponSlotPressed : a.WeaponSlotPressed;
            result.WeaponScrollDelta = b.WeaponScrollDelta != 0 ? b.WeaponScrollDelta : a.WeaponScrollDelta;
            result.EmotePressed = b.EmotePressed > 0 ? b.EmotePressed : a.EmotePressed;
            return result;
        }
    }
}
