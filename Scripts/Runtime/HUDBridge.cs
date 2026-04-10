using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Reads GameState each frame and updates the GameHUD.
    /// Godot port of the Unity HUDBridge.
    /// </summary>
    public partial class HUDBridge : Node
    {
        private GameState _state;
        private GameHUD _hud;
        private int _lastWeaponSlot = -1;
        private float _weaponNameTimer;
        private const float WeaponNameDuration = 1.5f;
        private int[] _cachedAmmo = new int[22];
        private bool _namesSet;

        public void Init(GameState state, GameHUD hud)
        {
            _state = state;
            _hud = hud;

            for (int i = 0; i < _cachedAmmo.Length; i++)
                _cachedAmmo[i] = int.MinValue;

            // Tell the HUD how many weapon slots to manage
            if (state?.Players != null && state.Players.Length > 0
                && state.Players[0].WeaponSlots != null)
                hud.SetTotalWeapons(state.Players[0].WeaponSlots.Length);
        }

        public override void _Process(double delta)
        {
            if (_state == null || _hud == null) return;
            if (_state.Players == null || _state.Players.Length == 0) return;

            ref PlayerState p1 = ref _state.Players[0];

            // Set player names once
            if (!_namesSet)
            {
                _namesSet = true;
                _hud.SetP1Name(p1.Name ?? "Player 1");
                if (_state.Players.Length > 1)
                    _hud.SetP2Name(_state.Players[1].Name ?? "Player 2");
            }

            // HP
            float hpNorm = p1.MaxHealth > 0 ? p1.Health / p1.MaxHealth : 0f;
            _hud.SetHPFill(hpNorm, p1.Health, p1.MaxHealth);

            // Energy
            if (p1.MaxEnergy > 0)
                _hud.SetEPFill(p1.Energy / p1.MaxEnergy, p1.Energy, p1.MaxEnergy);

            // Cooldown
            var weapon = p1.WeaponSlots[p1.ActiveWeaponSlot];
            if (weapon.ShootCooldown > 0)
            {
                float cooldownPercent = 1f - (p1.ShootCooldownRemaining / weapon.ShootCooldown);
                _hud.SetCooldownDisplay(Mathf.Clamp(cooldownPercent, 0f, 1f));
            }

            // Wind
            _hud.SetWind(_state.WindAngle, Mathf.Abs(_state.WindForce));

            // Weapon selection + ammo labels (only update on change)
            _hud.SelectWeapon(p1.ActiveWeaponSlot);
            int slotCount = p1.WeaponSlots != null ? p1.WeaponSlots.Length : 0;
            if (slotCount > _cachedAmmo.Length)
                System.Array.Resize(ref _cachedAmmo, slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                var slot = p1.WeaponSlots[i];
                if (slot.WeaponId == null) continue;
                if (slot.Ammo == _cachedAmmo[i]) continue;
                _cachedAmmo[i] = slot.Ammo;
                string label = slot.Ammo switch
                {
                    -1 => slot.WeaponId.ToUpper(),
                    0 => "EMPTY",
                    _ => $"{slot.WeaponId.ToUpper()} x{slot.Ammo}"
                };
                _hud.SetWeaponSlotAmmo(i, label);
            }

            // Weapon switch popup with details.
            // Suppressed during Waiting (countdown) so it doesn't overlap
            // the "3, 2, 1, GO!" label (#10).
            if (p1.ActiveWeaponSlot != _lastWeaponSlot
                && _state.Phase != MatchPhase.Waiting)
            {
                _lastWeaponSlot = p1.ActiveWeaponSlot;
                string name = weapon.WeaponId?.ToUpper() ?? "EMPTY";
                string ammo = weapon.Ammo >= 0 ? $"x{weapon.Ammo}" : "INF";
                string dmg = $"DMG:{weapon.MaxDamage:F0}";
                string special = weapon.Bounces > 0 ? " BOUNCE" : "";
                _hud.SetMatchState($"{name} [{ammo}] {dmg}{special}");
                _weaponNameTimer = WeaponNameDuration;
            }

            // Clear any lingering match state banner during countdown.
            if (_state.Phase == MatchPhase.Waiting)
            {
                _hud.SetMatchState("");
                _weaponNameTimer = 0f;
            }

            // Clear weapon name after timeout
            float dt = (float)delta;
            if (_weaponNameTimer > 0f)
            {
                _weaponNameTimer -= dt;
                if (_weaponNameTimer <= 0f && _state.Phase == MatchPhase.Playing)
                    _hud.SetMatchState("");
            }

            // Skill slots
            if (p1.SkillSlots != null)
            {
                for (int i = 0; i < p1.SkillSlots.Length && i < 2; i++)
                {
                    var skill = p1.SkillSlots[i];
                    if (skill.SkillId == null) continue;
                    _hud.SetSkillSlotName(i, skill.SkillId.ToUpper());
                    if (skill.IsActive)
                    {
                        _hud.SetSkillSlotCooldown(i, 1f, "ON");
                    }
                    else if (skill.CooldownRemaining > 0f)
                    {
                        float norm = 1f - (skill.CooldownRemaining / skill.Cooldown);
                        _hud.SetSkillSlotCooldown(i, norm,
                            skill.CooldownRemaining.ToString("F1"));
                    }
                    else
                    {
                        _hud.SetSkillSlotCooldown(i, 1f, "OK");
                    }
                }
            }

            // Player 2 HP
            if (_state.Players.Length > 1)
            {
                ref PlayerState p2 = ref _state.Players[1];
                float p2Hp = p2.MaxHealth > 0 ? p2.Health / p2.MaxHealth : 0f;
                _hud.SetP2HPFill(p2Hp);
            }

            // Payload mode: show timer and position
            if (_state.Config.MatchType == MatchType.Payload
                && _state.Phase == MatchPhase.Playing)
            {
                float timer = _state.Payload.MatchTimer;
                string timerStr = timer > 0f ? $"{timer:F0}s" : "SUDDEN DEATH";
                _hud.SetMatchState($"PAYLOAD {timerStr}");
            }

            // Match end
            if (_state.Phase == MatchPhase.Ended)
            {
                string winner = _state.WinnerIndex >= 0
                    ? _state.Players[_state.WinnerIndex].Name
                    : "Nobody";
                if (_state.Config.MatchType == MatchType.KingOfTheHill
                    && _state.Koth.Scores != null)
                {
                    float winScore = _state.WinnerIndex >= 0
                        ? _state.Koth.Scores[_state.WinnerIndex] : 0f;
                    _hud.SetMatchState($"{winner} Wins! (Score: {winScore:F0})");
                }
                else
                {
                    _hud.SetMatchState($"{winner} Wins!");
                }
            }
        }
    }
}
