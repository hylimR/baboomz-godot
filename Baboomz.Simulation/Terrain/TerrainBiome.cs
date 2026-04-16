namespace Baboomz.Simulation
{
    /// <summary>
    /// Defines terrain generation parameters for different biomes.
    /// Selected randomly each match for variety.
    /// </summary>
    public struct TerrainBiome
    {
        public string Name;
        public float MinHeight;
        public float MaxHeight;
        public float HillFrequency;
        // Colors used by the renderer (RGB floats)
        public float EarthR, EarthG, EarthB;
        public float SurfaceR, SurfaceG, SurfaceB;
        public BiomeHazardType HazardType;
        public int HazardCount; // number of hazards to spawn
        // Island mode: splits terrain into disconnected land masses
        public bool IslandMode;
        public int IslandCount;       // number of disconnected islands (2-4)
        public float IslandGapWidth;  // minimum horizontal gap between islands (world units)
        // Parallax background asset folder under Art/Backgrounds/. Renderer falls
        // back to "Default" if a layer PNG is missing here. Empty -> "Default".
        public string BackgroundFolder;

        public static readonly TerrainBiome[] All = new[]
        {
            new TerrainBiome
            {
                Name = "Grasslands",
                MinHeight = -5f, MaxHeight = 15f, HillFrequency = 0.1f,
                EarthR = 0.40f, EarthG = 0.26f, EarthB = 0.13f,
                SurfaceR = 0.48f, SurfaceG = 0.75f, SurfaceB = 0.26f,
                HazardType = BiomeHazardType.Mud, HazardCount = 3,
                BackgroundFolder = "Default"  // existing green-hills/blue-sky art matches
            },
            new TerrainBiome
            {
                Name = "Desert",
                MinHeight = -3f, MaxHeight = 10f, HillFrequency = 0.06f,
                EarthR = 0.76f, EarthG = 0.60f, EarthB = 0.35f,
                SurfaceR = 0.85f, SurfaceG = 0.72f, SurfaceB = 0.42f,
                HazardType = BiomeHazardType.Quicksand, HazardCount = 2,
                BackgroundFolder = "Desert"
            },
            new TerrainBiome
            {
                Name = "Arctic",
                MinHeight = -4f, MaxHeight = 12f, HillFrequency = 0.08f,
                EarthR = 0.55f, EarthG = 0.60f, EarthB = 0.65f,
                SurfaceR = 0.90f, SurfaceG = 0.93f, SurfaceB = 0.97f,
                HazardType = BiomeHazardType.Ice, HazardCount = 3,
                BackgroundFolder = "Arctic"
            },
            new TerrainBiome
            {
                Name = "Volcanic",
                MinHeight = -6f, MaxHeight = 18f, HillFrequency = 0.15f,
                EarthR = 0.25f, EarthG = 0.15f, EarthB = 0.10f,
                SurfaceR = 0.35f, SurfaceG = 0.20f, SurfaceB = 0.15f,
                HazardType = BiomeHazardType.Lava, HazardCount = 2,
                BackgroundFolder = "Volcanic"
            },
            new TerrainBiome
            {
                Name = "Candy",
                MinHeight = -4f, MaxHeight = 14f, HillFrequency = 0.12f,
                EarthR = 0.85f, EarthG = 0.45f, EarthB = 0.60f,
                SurfaceR = 0.60f, SurfaceG = 0.85f, SurfaceB = 0.90f,
                HazardType = BiomeHazardType.Bounce, HazardCount = 3,
                BackgroundFolder = "Candy"
            },
            new TerrainBiome
            {
                Name = "Chinatown",
                MinHeight = -4f, MaxHeight = 16f, HillFrequency = 0.14f,
                EarthR = 0.55f, EarthG = 0.27f, EarthB = 0.07f,
                SurfaceR = 0.75f, SurfaceG = 0.22f, SurfaceB = 0.17f,
                HazardType = BiomeHazardType.Firecracker, HazardCount = 4,
                BackgroundFolder = "Chinatown"
            },
            new TerrainBiome
            {
                Name = "Clockwork Foundry",
                MinHeight = -5f, MaxHeight = 17f, HillFrequency = 0.16f,
                EarthR = 0.23f, EarthG = 0.23f, EarthB = 0.25f,
                SurfaceR = 0.72f, SurfaceG = 0.45f, SurfaceB = 0.20f,
                HazardType = BiomeHazardType.Gear, HazardCount = 3,
                BackgroundFolder = "Steampunk"  // matches existing Art/Backgrounds/Steampunk/ folder
            },
            new TerrainBiome
            {
                Name = "Sunken Ruins",
                MinHeight = -6f, MaxHeight = 13f, HillFrequency = 0.07f,
                EarthR = 0.15f, EarthG = 0.35f, EarthB = 0.40f,
                SurfaceR = 0.80f, SurfaceG = 0.65f, SurfaceB = 0.45f,
                HazardType = BiomeHazardType.Whirlpool, HazardCount = 2,
                BackgroundFolder = "Sunken"
            },
            new TerrainBiome
            {
                Name = "Storm at Sea",
                MinHeight = -10f, MaxHeight = 6f, HillFrequency = 0.02f,
                EarthR = 0.25f, EarthG = 0.35f, EarthB = 0.45f,
                SurfaceR = 0.35f, SurfaceG = 0.50f, SurfaceB = 0.60f,
                HazardType = BiomeHazardType.Waterspout, HazardCount = 2,
                IslandMode = true, IslandCount = 3, IslandGapWidth = 5f,
                BackgroundFolder = "Storm"
            }
        };

        public static TerrainBiome GetRandom(int seed)
        {
            return All[((seed % All.Length) + All.Length) % All.Length];
        }
    }

    /// <summary>
    /// Applies map-wide gameplay modifiers based on biome weather.
    /// Mutates the runtime config copy so all systems pick up changes transparently.
    /// </summary>
    public static class BiomeModifiers
    {
        public static void Apply(GameConfig config, TerrainBiome biome)
        {
            // Save pre-biome values on first call; restore them on subsequent calls
            // so modifiers from a previous biome don't bleed into the next round
            if (!config.BiomeBaselineSaved)
            {
                config.BiomeBaselineSaved = true;
                config.BaseTerrainDestructionMult = config.TerrainDestructionMult;
                config.BaseWindChangeInterval = config.WindChangeInterval;
                config.BaseMaxWindStrength = config.MaxWindStrength;
                config.BaseMoveSpeedMult = config.MoveSpeedMult;
                config.BaseFallDamagePerMeter = config.FallDamagePerMeter;
                config.BaseFireZoneDurationMult = config.FireZoneDurationMult;
                config.BaseCrateSpawnInterval = config.CrateSpawnInterval;
                config.BaseDefaultEnergyRegen = config.DefaultEnergyRegen;
                config.BaseKnockbackMult = config.KnockbackMult;
                config.BaseGravity = config.Gravity;
                config.BaseDefaultCooldownMultiplier = config.DefaultCooldownMultiplier;
                config.BaseWaterRiseSpeed = config.WaterRiseSpeed;
            }
            else
            {
                config.TerrainDestructionMult = config.BaseTerrainDestructionMult;
                config.WindChangeInterval = config.BaseWindChangeInterval;
                config.MaxWindStrength = config.BaseMaxWindStrength;
                config.MoveSpeedMult = config.BaseMoveSpeedMult;
                config.FallDamagePerMeter = config.BaseFallDamagePerMeter;
                config.FireZoneDurationMult = config.BaseFireZoneDurationMult;
                config.CrateSpawnInterval = config.BaseCrateSpawnInterval;
                config.DefaultEnergyRegen = config.BaseDefaultEnergyRegen;
                config.KnockbackMult = config.BaseKnockbackMult;
                config.Gravity = config.BaseGravity;
                config.DefaultCooldownMultiplier = config.BaseDefaultCooldownMultiplier;
                config.WaterRiseSpeed = config.BaseWaterRiseSpeed;
            }

            switch (biome.Name)
            {
                case "Grasslands":
                    config.TerrainDestructionMult = 1.3f;
                    break;
                case "Desert":
                    config.WindChangeInterval = 5f;
                    config.MaxWindStrength = 4.5f;
                    break;
                case "Arctic":
                    config.MoveSpeedMult = 0.85f;
                    config.FallDamagePerMeter = 4f;
                    break;
                case "Volcanic":
                    config.FireZoneDurationMult = 1.5f;
                    break;
                case "Candy":
                    config.CrateSpawnInterval = 10f;
                    config.DefaultEnergyRegen = 13f;
                    break;
                case "Chinatown":
                    config.KnockbackMult = 1.3f;
                    break;
                case "Clockwork Foundry":
                    config.DefaultCooldownMultiplier = 0.8f;
                    break;
                case "Sunken Ruins":
                    config.Gravity *= 0.75f;
                    break;
                case "Storm at Sea":
                    config.WindChangeInterval = 2f;
                    config.MaxWindStrength = 6f;
                    config.WaterRiseSpeed *= 1.5f;
                    break;
            }
        }

        public static string GetModifierHint(string biomeName)
        {
            switch (biomeName)
            {
                case "Grasslands": return "Rain softens the earth... (Terrain destruction +30%)";
                case "Desert": return "Sandstorms shift unpredictably... (Wind changes faster)";
                case "Arctic": return "Deep snow slows movement... (Move speed -15%, fall damage halved)";
                case "Volcanic": return "Everything burns longer... (Fire zone duration +50%)";
                case "Candy": return "Sugar rush! (Crate spawns 2x, energy regen +30%)";
                case "Chinatown": return "Festival chaos! (Knockback force +30%)";
                case "Clockwork Foundry": return "Factory turbines accelerate everything... (Weapon cooldowns -20%)";
                case "Sunken Ruins": return "Water lifts everything... (Gravity -25%, shots arc wider)";
                case "Storm at Sea": return "Storm surge approaches... (Extreme wind, water rises faster)";
                default: return null;
            }
        }
    }
}
