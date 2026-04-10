using System;

namespace Baboomz
{
    /// <summary>
    /// Serializable classes mirroring the level JSON schema for JsonUtility deserialization.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public string id;
        public string name;
        public LevelTerrainData terrain;
        public LevelSpawnData playerSpawn;
        public LevelEnemyData[] enemies = Array.Empty<LevelEnemyData>();
        public LevelStructureData[] structures = Array.Empty<LevelStructureData>();
        public LevelObjectiveData objectives;

        // Campaign fields
        public string worldId;
        public int worldIndex;
        public int levelIndex;
        public float parTime = 120f;
        public string introDialog = "";
        public string victoryDialog = "";
        public LevelDifficultyData difficulty;
        public LevelBossData boss;
        public LevelHiddenObjective hiddenObjective;

        // Tutorial fields
        public bool isTutorial;
        public LevelTutorialStepData[] tutorialSteps;
    }

    [Serializable]
    public class LevelTerrainData
    {
        public int seed = 12345;
        public int mapWidth = 200;
        public float minHeight = 0.3f;
        public float maxHeight = 0.7f;
        public float hillFrequency = 1.5f;
        public int islandCount = 0;
        public float indestructibleFloorHeight = 0f;
    }

    [Serializable]
    public class LevelSpawnData
    {
        public float x;
        public float groundOffset = 2f;
    }

    [Serializable]
    public class LevelEnemyData
    {
        public string type = "turret";
        public float x;
        public float groundOffset = 2f;
        public string difficulty = "normal";
        public float hpOverride = -1f;      // -1 = use mob default * difficulty multiplier
        public float damageOverride = -1f;   // -1 = use mob default * difficulty multiplier
    }

    [Serializable]
    public class LevelStructureData
    {
        public string type = "platform";
        public float x;
        public float y;
        public float width = 10f;
        public float height = 2f;
    }

    [Serializable]
    public class LevelObjectiveData
    {
        public string type = "eliminate_all";  // eliminate_all, defeat_boss, survive_time, survive_waves, destroy_target
        public float timeLimit = 0f;           // survive_time: seconds to survive
        public int waveCount = 0;              // survive_waves: number of waves
        public int targetCount = 0;            // destroy_target: structures to destroy
        public string bossType = "";           // defeat_boss: boss AI identifier
        public LevelWaveData[] waves;          // survive_waves: wave definitions
    }

    [Serializable]
    public class LevelWaveData
    {
        public LevelEnemyData[] enemies;
        public float delay = 5f;               // seconds after previous wave before this one spawns
    }

    [Serializable]
    public class LevelDifficultyData
    {
        public float hpMultiplier = 1f;
        public float damageMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float windStrengthOverride = -1f;   // -1 = use default
        public int mineCountOverride = -1;          // -1 = use default
    }

    [Serializable]
    public class LevelBossData
    {
        public string bossType;
        public float bossHP = 200f;
        public float bossX;
        public float[] phaseThresholds;              // HP fractions that trigger phase changes (e.g. 0.66, 0.33)
        public LevelEnemyData[] reinforcements;      // spawned at each phase threshold
    }

    [Serializable]
    public class LevelHiddenObjective
    {
        public string type = "";               // no_mines_triggered, no_damage_taken, speedrun, cannon_only
        public float value = 0f;               // threshold if applicable
        public int bonusCurrency = 50;
    }

    [Serializable]
    public class LevelTutorialStepData
    {
        public int stepId;
        public string title;
        public string description;
        public string actionType;     // move_right, jump, aim_up, charge_and_fire, switch_weapon, use_skill, destroy_terrain, kill_enemy
        public float threshold = 1f;  // completion threshold
        public int targetWeaponSlot = -1;
        public int targetSkillSlot = -1;
    }
}
