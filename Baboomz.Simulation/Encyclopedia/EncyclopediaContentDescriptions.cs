namespace Baboomz.Simulation
{
    /// <summary>
    /// Description and lore text lookups for weapons and skills.
    /// Split from EncyclopediaContent.cs to comply with 300-line SOLID limit.
    /// </summary>
    public static partial class EncyclopediaContent
    {
        public static string GetWeaponLore(string weaponId, LoreData lore)
        {
            if (lore?.weapons == null) return null;
            foreach (var weapon in lore.weapons)
            {
                if (weapon.id == weaponId) return weapon.origin;
            }
            return null;
        }

        public static string GetWeaponDescription(string weaponId)
        {
            switch (weaponId)
            {
                case "cannon": return "Reliable standard weapon with unlimited ammo. Low damage but always available.";
                case "shotgun": return "Fires a 4-pellet spread. Devastating at close range, weak at distance.";
                case "rocket": return "High-damage explosive with a large blast radius. Limited ammo.";
                case "cluster": return "Splits into 4 sub-projectiles on impact for area denial.";
                case "dynamite": return "Bouncing explosive with a timed fuse. Massive damage in a wide radius.";
                case "napalm": return "Creates a lingering fire zone on impact that burns over time.";
                case "airstrike": return "Marks a target location, then drops 5 bombs from above.";
                case "drill": return "Tunnels straight through terrain. Great for digging out entrenched enemies.";
                case "blowtorch": return "Rapid-fire terrain cutter. Low damage but carves quickly.";
                case "holy_hand_grenade": return "The ultimate explosive. Destroys everything — including indestructible terrain.";
                case "sheep": return "A walking projectile that follows the terrain surface before detonating.";
                case "banana_bomb": return "Splits into 6 sub-projectiles for maximum area coverage.";
                case "freeze_grenade": return "Freezes targets in place for 2 seconds. Minimal damage but huge tactical value.";
                case "sticky_bomb": return "Sticks to surfaces or players, then detonates after a short fuse.";
                case "lightning_rod": return "Instant-hit hitscan beam that chains to a nearby secondary target.";
                case "boomerang": return "A returning projectile that can hit on both the outward and return trip.";
                case "gravity_bomb": return "Sticks on impact and creates a vortex that pulls nearby players toward it before detonating.";
                case "ricochet_disc": return "A fast disc that bounces off terrain up to 3 times, dealing damage at each bounce point.";
                case "magma_ball": return "A slow lava projectile that creates a molten pool on impact, progressively melting terrain underneath.";
                case "gust_cannon": return "A zero-damage wind blast that launches enemies with extreme knockback. Infinite ammo utility weapon.";
                case "harpoon": return "A piercing projectile that punches through the first target it hits and continues traveling. High single-target damage with limited ammo.";
                case "flak_cannon": return "Fires a shell that detonates mid-air and rains 8 fragments downward. Charge distance determines the burst point — shorter charge, closer burst.";
                default: return "Unknown weapon.";
            }
        }

        public static string GetSkillDescription(string skillId)
        {
            switch (skillId)
            {
                case "teleport": return "Instantly teleport to the cursor position. Great for escaping or repositioning.";
                case "grapple": return "Swing from a rope anchor point with pendulum physics. Release to launch with momentum.";
                case "shield": return "Activate a temporary damage-absorbing shield around yourself.";
                case "dash": return "A quick burst of horizontal speed to dodge or close distance.";
                case "heal": return "Gradually restore health over a short duration.";
                case "jetpack": return "Fly upward for a brief period to reach high ground or dodge attacks.";
                case "girder": return "Place an indestructible platform to create cover or bridge gaps.";
                case "earthquake": return "Shake the ground, dealing damage to all grounded enemies on the map.";
                case "smoke": return "Deploy a smoke screen that obstructs vision in the target area.";
                case "warcry": return "Boost damage output for yourself and nearby teammates.";
                case "mine_layer": return "Place a proximity mine that detonates when an enemy walks near it.";
                case "energy_drain": return "Steal energy from the nearest enemy within range.";
                case "deflect": return "Activate a brief deflection field that reflects incoming projectiles back at the shooter.";
                case "decoy": return "Create a hologram at your position and become invisible for 2 seconds. AI targets the decoy while you reposition.";
                case "hookshot": return "Pull the nearest enemy toward you, dealing damage and displacing them for combo setups.";
                case "shadow_step": return "Mark your current position, fight freely, then snap back to the marked spot when the duration expires.";
                case "overcharge": return "Commit all your energy for a charged burst. Your next shot within 5 seconds deals double damage.";
                case "mend": return "Refill destroyed terrain in a small radius near your aim target. Restores cover without creating indestructible blocks.";
                case "magnetic_mine": return "Deploy a proximity mine that slowly crawls toward the nearest enemy. Dormant for 1 second, then seeks targets within detection range.";
                default: return "Unknown skill.";
            }
        }

        public static string GetSkillEffectDescription(string skillId, float value)
        {
            switch (skillId)
            {
                case "teleport": return "Instant repositioning";
                case "grapple": return "Swing on rope, launch with momentum";
                case "shield": return $"{value}x armor for duration";
                case "dash": return $"Burst of speed ({value} velocity)";
                case "heal": return $"Restores {value} HP over time";
                case "jetpack": return $"Upward thrust ({value} force)";
                case "girder": return "Places indestructible platform";
                case "earthquake": return $"{value} damage to all grounded enemies";
                case "smoke": return "Obstructs vision in area";
                case "warcry": return $"{value}x damage buff for team";
                case "mine_layer": return $"Places proximity mine ({value} damage)";
                case "energy_drain": return $"Steals {value} energy from nearby enemy";
                case "deflect": return "Reflects projectiles back at shooter";
                case "decoy": return "Hologram + invisibility for repositioning";
                case "hookshot": return $"Pulls enemy toward you ({value} damage)";
                case "shadow_step": return "Mark position, recall after duration";
                case "overcharge": return $"Drain all energy; next shot deals {value}x damage";
                case "mend": return $"Refills destroyed terrain ({value}-unit radius)";
                case "magnetic_mine": return $"Homing mine ({value} damage, crawls toward enemies)";
                default: return "Unknown effect";
            }
        }
    }
}
