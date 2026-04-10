using System.Collections.Generic;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Static encyclopedia content: mob, boss, biome entry collections.
    /// Split from EncyclopediaData to comply with 300-line SOLID limit.
    /// Description lookups in EncyclopediaContentDescriptions.cs (partial).
    /// Faction/history entries in EncyclopediaContentLore.cs (partial).
    /// </summary>
    public static partial class EncyclopediaContent
    {
        public static EncyclopediaEntry[] GetMobEntries()
        {
            return new[]
            {
                new EncyclopediaEntry
                {
                    Id = "bomber", Name = "Bomber",
                    Description = "Lobs bouncing grenades from medium range. Flees if you get too close.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Behavior", "Ranged" },
                        { "Special", "Bouncing grenades" },
                        { "Weakness", "Close-range rush" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "shielder", Name = "Shielder",
                    Description = "Advances slowly with a frontal shield that absorbs damage. Attack from behind or above.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Behavior", "Melee" },
                        { "Special", "Frontal shield" },
                        { "Weakness", "Flanking / aerial attacks" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "flyer", Name = "Flyer",
                    Description = "Hovers above terrain firing rapid weak shots. Ignores ground obstacles.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Behavior", "Aerial" },
                        { "Special", "Terrain bypass" },
                        { "Weakness", "Hitscan weapons" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "healer", Name = "Healer",
                    Description = "Heals nearby allies and flees from players. No direct attacks — eliminate first to cut enemy sustain.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Behavior", "Support" },
                        { "Special", "Heals allies" },
                        { "Weakness", "Priority target" }
                    }
                }
            };
        }

        public static EncyclopediaEntry[] GetBossEntries()
        {
            return new[]
            {
                new EncyclopediaEntry
                {
                    Id = "iron_sentinel", Name = "Iron Sentinel",
                    Description = "Stationary turret boss that rotates and fires volleys. Enters overdrive at low HP.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Phases", "3" },
                        { "Type", "Turret" },
                        { "Tip", "Stay mobile between volleys" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "sand_wyrm", Name = "Sand Wyrm",
                    Description = "Burrows underground and surfaces to strike. Destroys terrain when emerging.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Phases", "3" },
                        { "Type", "Burrower" },
                        { "Tip", "Watch for dust clouds before it surfaces" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "glacial_cannon", Name = "Glacial Cannon",
                    Description = "Ice-themed boss that fires freeze projectiles and creates ice patches.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Phases", "3" },
                        { "Type", "Artillery" },
                        { "Tip", "Avoid ice patches, use fire weapons" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "forge_colossus", Name = "Forge Colossus",
                    Description = "Massive mech boss that slams the ground and fires heated projectiles.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Phases", "3" },
                        { "Type", "Mech" },
                        { "Tip", "Jump over ground slams, attack during cooldowns" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "baron_cogsworth", Name = "Baron Cogsworth",
                    Description = "Final boss with gear-based attacks and summoning abilities. Multi-phase fight with increasing complexity.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Phases", "4" },
                        { "Type", "Commander" },
                        { "Tip", "Destroy summoned minions before focusing the baron" }
                    }
                }
            };
        }

        public static EncyclopediaEntry[] GetBiomeEntries()
        {
            return new[]
            {
                new EncyclopediaEntry
                {
                    Id = "grasslands", Name = "Grasslands",
                    Description = "Lush green hills with moderate terrain. A balanced starting biome.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Hazard", "Mud (halves movement speed)" },
                        { "Terrain", "Rolling hills, medium height" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "desert", Name = "Desert",
                    Description = "Flat sandy dunes with sparse cover. Watch for quicksand pits.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Hazard", "Quicksand (pulls player down)" },
                        { "Terrain", "Low, gentle dunes" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "arctic", Name = "Arctic",
                    Description = "Frozen tundra with icy surfaces. Reduced friction makes movement tricky.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Hazard", "Ice (zero friction)" },
                        { "Terrain", "Medium height, smooth curves" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "volcanic", Name = "Volcanic",
                    Description = "Jagged volcanic peaks with lava pools. High terrain with dangerous hazards.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Hazard", "Lava (deals damage over time)" },
                        { "Terrain", "Tall, sharp peaks" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "candy", Name = "Candy",
                    Description = "Colorful candy-themed landscape. Bouncy surfaces launch players skyward.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Hazard", "Bounce (launches player upward)" },
                        { "Terrain", "Medium height, playful curves" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "chinatown", Name = "Chinatown",
                    Description = "Neon-lit rooftops with pagoda spires and festival decorations. Firecrackers create unpredictable knockback zones.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Hazard", "Firecrackers (random knockback)" },
                        { "Terrain", "Flat rooftops with gaps" },
                        { "Modifier", "Knockback +30%" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "clockwork_foundry", Name = "Clockwork Foundry",
                    Description = "Steampunk factory with jagged metallic terrain. Spinning gears push players sideways.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Hazard", "Gear (pushes player horizontally)" },
                        { "Terrain", "Tall angular peaks, short sight lines" },
                        { "Modifier", "Cooldown -20%" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "sunken_ruins", Name = "Sunken Ruins",
                    Description = "Ancient underwater ruins with reduced gravity and whirlpool hazards. Shots arc wider and jumps soar higher.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Hazard", "Whirlpool (pulls player toward center)" },
                        { "Terrain", "Smooth rolling formations, low peaks" },
                        { "Modifier", "Gravity -25%" }
                    }
                }
            };
        }
    }
}
