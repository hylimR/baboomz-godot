using System;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>
    /// Persistent player data saved to JSON.
    /// Contains currency, unlocked weapons, level completions, and upgrade levels.
    /// Pure data class -- no engine dependencies.
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        public int currency;
        public List<string> unlockedWeapons = new List<string>();
        public List<LevelCompletion> levelCompletions = new List<LevelCompletion>();
        public List<UpgradeState> upgrades = new List<UpgradeState>();

        /// <summary>
        /// Returns the completion record for a level, or null if never completed.
        /// </summary>
        public LevelCompletion GetCompletion(string levelId)
        {
            for (int i = 0; i < levelCompletions.Count; i++)
            {
                if (levelCompletions[i].levelId == levelId)
                    return levelCompletions[i];
            }
            return null;
        }

        /// <summary>
        /// Sets or updates the completion for a level. Keeps the best star count.
        /// </summary>
        public void SetCompletion(string levelId, int stars)
        {
            LevelCompletion existing = GetCompletion(levelId);
            if (existing != null)
            {
                if (stars > existing.stars)
                    existing.stars = stars;
            }
            else
            {
                levelCompletions.Add(new LevelCompletion { levelId = levelId, stars = stars });
            }
        }

        /// <summary>
        /// Returns the upgrade level for a given upgrade ID, defaulting to 0.
        /// </summary>
        public int GetUpgradeLevel(string upgradeId)
        {
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].upgradeId == upgradeId)
                    return upgrades[i].level;
            }
            return 0;
        }

        /// <summary>
        /// Sets the upgrade level for a given upgrade ID.
        /// </summary>
        public void SetUpgradeLevel(string upgradeId, int level)
        {
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].upgradeId == upgradeId)
                {
                    upgrades[i].level = level;
                    return;
                }
            }
            upgrades.Add(new UpgradeState { upgradeId = upgradeId, level = level });
        }

        public bool IsWeaponUnlocked(string weaponId)
        {
            return unlockedWeapons.Contains(weaponId);
        }
    }

    [Serializable]
    public class LevelCompletion
    {
        public string levelId;
        public int stars; // 0-3
    }

    [Serializable]
    public class UpgradeState
    {
        public string upgradeId;
        public int level;
    }
}
