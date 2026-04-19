using System;

namespace Baboomz.Simulation
{
    /// <summary>Handles skill activation and per-frame updates for duration-based skills.</summary>
    public static partial class SkillSystem
    {
        public static void ActivateSkill(GameState state, int playerIndex, int skillSlot)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            if (p.IsDead) return;
            if (p.FreezeTimer > 0f) return;
            if (p.RetreatTimer > 0f) return;
            if (p.SkillSlots == null) return;
            if (skillSlot < 0 || skillSlot >= p.SkillSlots.Length) return;

            ref SkillSlotState skill = ref p.SkillSlots[skillSlot];
            if (skill.SkillId == null) return;

            // Shadow Step early return: re-activating while active triggers recall
            // Must be checked before cooldown guard (cooldown is running during active window)
            if (skill.IsActive && skill.Type == SkillType.ShadowStep)
            {
                DeactivateSkill(state, ref p, ref skill, playerIndex);
                return;
            }

            // Ninja Rope: detach mid-swing or re-hook during window
            if (skill.Type == SkillType.GrapplingHook
                && TryHandleRopeActivation(state, ref p, ref skill, playerIndex))
                return;

            if (skill.CooldownRemaining > 0f) return;
            if (skill.IsActive) return;

            // Block activation if any other duration-based skill is already active
            for (int i = 0; i < p.SkillSlots.Length; i++)
                if (p.SkillSlots[i].IsActive) return;

            if (skill.EnergyCost > 0f && p.Energy < skill.EnergyCost) return;

            // Deduct energy
            if (skill.EnergyCost > 0f)
                p.Energy -= skill.EnergyCost;

            Vec2 origin = p.Position;

            switch (skill.Type)
            {
                case SkillType.Teleport:
                    ExecuteTeleport(state, ref p, ref skill);
                    break;

                case SkillType.GrapplingHook:
                    if (!ExecuteGrappleStart(state, ref p, ref skill))
                    {
                        // No terrain hit — refund energy, skip cooldown
                        if (skill.EnergyCost > 0f)
                            p.Energy += skill.EnergyCost;
                        return;
                    }
                    p.RopeHookCount = 0; // fresh activation resets hook count
                    p.RopeRehookWindow = 0f;
                    break;

                case SkillType.Shield:
                    skill.Value2 = p.ArmorMultiplier; // store original
                    p.ArmorMultiplier = skill.Value;
                    skill.IsActive = true;
                    skill.DurationRemaining = skill.Duration;
                    break;

                case SkillType.Dash:
                    p.Velocity.x = skill.Value * p.FacingDirection;
                    skill.IsActive = true;
                    skill.DurationRemaining = skill.Duration;
                    break;

                case SkillType.Heal:
                    skill.IsActive = true;
                    skill.DurationRemaining = skill.Duration;
                    break;

                case SkillType.Jetpack:
                    skill.IsActive = true;
                    skill.DurationRemaining = skill.Duration;
                    break;

                case SkillType.Girder:
                    ExecuteGirder(state, ref p, ref skill);
                    break;

                case SkillType.Earthquake:
                    ExecuteEarthquake(state, playerIndex, skill.Value);
                    break;

                case SkillType.SmokeScreen:
                    ExecuteSmokeScreen(state, ref p, ref skill);
                    break;

                case SkillType.WarCry:
                    ExecuteWarCry(state, playerIndex, ref skill);
                    break;

                case SkillType.MineLay:
                    ExecuteMineLay(state, playerIndex, ref p, ref skill);
                    break;

                case SkillType.EnergyDrain:
                    if (!ExecuteEnergyDrain(state, playerIndex, ref p, ref skill))
                    {
                        // No valid target — refund energy, skip cooldown and event
                        if (skill.EnergyCost > 0f)
                            p.Energy += skill.EnergyCost;
                        return;
                    }
                    break;

                case SkillType.Deflect:
                    p.DeflectTimer = skill.Duration;
                    skill.IsActive = true;
                    skill.DurationRemaining = skill.Duration;
                    break;

                case SkillType.Decoy:
                    ExecuteDecoy(state, ref p, ref skill);
                    break;

                case SkillType.HookShot:
                    if (!ExecuteHookShot(state, playerIndex, ref p, ref skill))
                    {
                        if (skill.EnergyCost > 0f)
                            p.Energy += skill.EnergyCost;
                        return;
                    }
                    break;

                case SkillType.ShadowStep:
                    ExecuteShadowStep(ref p, ref skill);
                    break;

                case SkillType.Overcharge:
                    if (!ExecuteOvercharge(state, ref p, ref skill))
                    {
                        // Min-energy gate failed — refund energy cost, skip cooldown
                        if (skill.EnergyCost > 0f)
                            p.Energy += skill.EnergyCost;
                        return;
                    }
                    break;

                case SkillType.Mend:
                    ExecuteMend(state, ref p, ref skill);
                    break;

                case SkillType.MagneticMine:
                    ExecuteMagneticMine(state, playerIndex, ref p, ref skill);
                    break;

                case SkillType.Petrify:
                    ExecutePetrify(state, playerIndex, ref p, ref skill);
                    break;

                case SkillType.Sprint:
                    ExecuteSprint(ref p, ref skill);
                    break;

                case SkillType.Landslide:
                    ExecuteLandslide(state, ref p, ref skill);
                    break;
            }

            // Start cooldown (apply CooldownMultiplier so skills respect haste/slow modifiers)
            skill.CooldownRemaining = skill.Cooldown * p.CooldownMultiplier;

            // Emit event for renderer
            state.SkillEvents.Add(new SkillEvent
            {
                PlayerIndex = playerIndex,
                Type = skill.Type,
                Position = origin,
                TargetPosition = p.Position
            });

            if (state.SkillsActivated != null && playerIndex < state.SkillsActivated.Length)
                state.SkillsActivated[playerIndex].Add(skill.Type);
        }

        public static void Update(GameState state, float dt)
        {
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (p.SkillSlots == null) continue;

                TickRopeRehookWindow(ref p, dt);

                // Deactivate all skills on death
                if (p.IsDead)
                {
                    for (int s = 0; s < p.SkillSlots.Length; s++)
                    {
                        if (p.SkillSlots[s].IsActive)
                            DeactivateSkill(state, ref p, ref p.SkillSlots[s], i);
                    }
                    continue;
                }

                for (int s = 0; s < p.SkillSlots.Length; s++)
                {
                    ref SkillSlotState skill = ref p.SkillSlots[s];
                    if (skill.SkillId == null) continue;

                    // Tick cooldown
                    if (skill.CooldownRemaining > 0f)
                    {
                        skill.CooldownRemaining -= dt;
                        if (skill.CooldownRemaining < 0f)
                            skill.CooldownRemaining = 0f;
                    }

                    // Tick active skills
                    if (!skill.IsActive) continue;

                    UpdateActiveSkill(state, ref p, ref skill, dt, i);

                    skill.DurationRemaining -= dt;
                    if (skill.DurationRemaining <= 0f)
                        DeactivateSkill(state, ref p, ref skill, i);
                }
            }

            // Tick smoke zones
            UpdateSmokeZones(state, dt);
        }

    }
}
