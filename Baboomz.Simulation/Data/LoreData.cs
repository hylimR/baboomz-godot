using System;

namespace Baboomz
{
    [Serializable]
    public class LoreData
    {
        public LoreWorldData world;
        public LoreCharacterData[] characters = Array.Empty<LoreCharacterData>();
        public LoreBossData[] bosses = Array.Empty<LoreBossData>();
        public LoreMobData[] mobs = Array.Empty<LoreMobData>();
        public LoreBiomeData[] biomes = Array.Empty<LoreBiomeData>();
        public LoreWeaponData[] weapons = Array.Empty<LoreWeaponData>();
        public LoreFactionData[] factions = Array.Empty<LoreFactionData>();
        public LoreHistoryData[] history = Array.Empty<LoreHistoryData>();
    }

    [Serializable]
    public class LoreWorldData
    {
        public string name;
        public string description;
    }

    [Serializable]
    public class LoreCharacterData
    {
        public string id;
        public string name;
        public string role;
        public string description;
        public string personality;
    }

    [Serializable]
    public class LoreBossData
    {
        public string id;
        public string name;
        public int worldIndex;
        public string description;
        public string title;
        public string quote;
    }

    [Serializable]
    public class LoreMobData
    {
        public string id;
        public string name;
        public string description;
    }

    [Serializable]
    public class LoreBiomeData
    {
        public string id;
        public string name;
        public int worldIndex;
        public string terrain;
        public string description;
        public string thematicArc;
    }

    [Serializable]
    public class LoreWeaponData
    {
        public string id;
        public string name;
        public string origin;
    }

    [Serializable]
    public class LoreFactionData
    {
        public string id;
        public string name;
        public string description;
        public string quote;
    }

    [Serializable]
    public class LoreHistoryData
    {
        public string id;
        public string title;
        public string description;
    }
}
