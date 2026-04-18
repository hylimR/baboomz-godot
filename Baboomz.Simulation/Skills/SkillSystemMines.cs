using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Mine placement skills: standard mine lay and magnetic (homing) mine.
    /// Partial class extension of SkillSystem — extracted from SkillSystemEnvironmental.cs.
    /// </summary>
    public static partial class SkillSystem
    {
        static void ExecuteMineLay(GameState state, int playerIndex, ref PlayerState p, ref SkillSlotState skill)
        {
            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 direction = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad));
            float range = Math.Min(skill.Range, 10f);
            Vec2 target = p.Position + direction * range;

            // Raycast to find terrain surface
            Vec2 minePos;
            if (GamePhysics.RaycastTerrain(state.Terrain, p.Position + new Vec2(0f, 0.5f), target, out Vec2 hitPoint))
                minePos = hitPoint + new Vec2(0f, 0.1f);
            else
                minePos = target; // no terrain hit — place at max range

            // Enforce max 3 active mines per player (#125). Select the mine with the lowest
            // PlacedTime — this is the genuinely oldest, independent of list position
            // (list index is unstable because slots can be reused after explosions). (#33)
            const int MaxActiveMinesPerPlayer = 3;
            int ownedCount = 0;
            int oldestIndex = -1;
            float oldestPlacedTime = float.MaxValue;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (state.Mines[i].Active && state.Mines[i].OwnerIndex == playerIndex)
                {
                    ownedCount++;
                    if (state.Mines[i].PlacedTime < oldestPlacedTime)
                    {
                        oldestPlacedTime = state.Mines[i].PlacedTime;
                        oldestIndex = i;
                    }
                }
            }
            if (ownedCount >= MaxActiveMinesPerPlayer && oldestIndex >= 0)
            {
                var oldest = state.Mines[oldestIndex];
                oldest.Active = false;
                state.Mines[oldestIndex] = oldest;
            }

            state.Mines.Add(new MineState
            {
                Position = minePos,
                TriggerRadius = 1.2f,
                ExplosionRadius = 2.5f,
                Damage = skill.Value > 0f ? skill.Value : 35f,
                Active = true,
                Lifetime = 15f,
                OwnerIndex = playerIndex,
                PlacedTime = state.Time
            });
        }

        static void ExecuteMagneticMine(GameState state, int playerIndex, ref PlayerState p, ref SkillSlotState skill)
        {
            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 direction = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad));
            float range = Math.Min(skill.Range, 10f);
            Vec2 target = p.Position + direction * range;

            Vec2 minePos;
            if (GamePhysics.RaycastTerrain(state.Terrain, p.Position + new Vec2(0f, 0.5f), target, out Vec2 hitPoint))
                minePos = hitPoint + new Vec2(0f, 0.1f);
            else
                minePos = target;

            const int MaxMagneticMinesPerPlayer = 2;
            int ownedCount = 0;
            int oldestIndex = -1;
            float oldestPlacedTime = float.MaxValue;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (state.Mines[i].Active && state.Mines[i].IsHoming && state.Mines[i].OwnerIndex == playerIndex)
                {
                    ownedCount++;
                    if (state.Mines[i].PlacedTime < oldestPlacedTime)
                    {
                        oldestPlacedTime = state.Mines[i].PlacedTime;
                        oldestIndex = i;
                    }
                }
            }
            if (ownedCount >= MaxMagneticMinesPerPlayer && oldestIndex >= 0)
            {
                var oldest = state.Mines[oldestIndex];
                oldest.Active = false;
                state.Mines[oldestIndex] = oldest;
            }

            state.Mines.Add(new MineState
            {
                Position = minePos,
                TriggerRadius = 1.0f,
                ExplosionRadius = 2.5f,
                Damage = skill.Value > 0f ? skill.Value : 30f,
                Active = true,
                Lifetime = 12f,
                OwnerIndex = playerIndex,
                PlacedTime = state.Time,
                IsHoming = true,
                DetectionRange = 8f,
                MoveSpeed = 1.5f,
                ActivationDelay = 1f
            });
        }
    }
}
