using Godot;

namespace Baboomz
{
    /// <summary>
    /// Business logic for campaign progression: level completion, unlocks, currency, stars.
    /// Plain C# class -- not a Node. All calculation methods are static and pure for testability.
    /// Replaces Unity's VContainer-registered ProgressionService.
    /// </summary>
    public class ProgressionService
    {
        private PlayerSaveData saveData;

        public PlayerSaveData SaveData => saveData;

        public ProgressionService()
        {
            saveData = SaveManager.Load();
            EnsureDefaults();
        }

        /// <summary>
        /// Ensures the default weapon is always unlocked.
        /// </summary>
        private void EnsureDefaults()
        {
            if (!saveData.IsWeaponUnlocked("cannon_basic"))
            {
                saveData.unlockedWeapons.Add("cannon_basic");
            }
        }

        /// <summary>
        /// Records level completion: calculates stars and currency, saves to disk.
        /// Returns the number of stars earned.
        /// </summary>
        public int CompleteLevel(string levelId, float healthPercent, float timeSeconds, float parTime)
        {
            int stars = CalculateStars(healthPercent, timeSeconds, parTime);
            int reward = CalculateCurrencyReward(stars);

            saveData.SetCompletion(levelId, stars);
            saveData.currency += reward;
            SaveManager.Save(saveData);

            GD.Print($"ProgressionService: Completed '{levelId}' -- {stars} stars, +{reward} currency (total: {saveData.currency})");
            return stars;
        }

        /// <summary>
        /// Returns true if the level is unlocked for play.
        /// A level is unlocked if all its required levels have been completed (>= 1 star).
        /// </summary>
        public bool IsLevelUnlocked(string levelId, string[] requiredLevelIds)
        {
            if (requiredLevelIds == null || requiredLevelIds.Length == 0)
                return true;

            for (int i = 0; i < requiredLevelIds.Length; i++)
            {
                LevelCompletion completion = saveData.GetCompletion(requiredLevelIds[i]);
                if (completion == null || completion.stars < 1)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Attempts to unlock a weapon. Returns true if successful.
        /// Fails if already unlocked or insufficient currency.
        /// </summary>
        public bool TryUnlockWeapon(string weaponId, int cost)
        {
            if (saveData.IsWeaponUnlocked(weaponId))
                return false;
            if (saveData.currency < cost)
                return false;

            saveData.currency -= cost;
            saveData.unlockedWeapons.Add(weaponId);
            SaveManager.Save(saveData);
            return true;
        }

        /// <summary>
        /// Attempts to purchase an upgrade level. Returns true if successful.
        /// </summary>
        public bool TryPurchaseUpgrade(string upgradeId, int cost, int maxLevel)
        {
            int currentLevel = saveData.GetUpgradeLevel(upgradeId);
            if (currentLevel >= maxLevel)
                return false;
            if (saveData.currency < cost)
                return false;

            saveData.currency -= cost;
            saveData.SetUpgradeLevel(upgradeId, currentLevel + 1);
            SaveManager.Save(saveData);
            return true;
        }

        /// <summary>
        /// Returns the best star count for a level, or 0 if never completed.
        /// </summary>
        public int GetLevelStars(string levelId)
        {
            LevelCompletion completion = saveData.GetCompletion(levelId);
            return completion != null ? completion.stars : 0;
        }

        /// <summary>
        /// Reloads save data from disk (e.g. after external changes).
        /// </summary>
        public void Reload()
        {
            saveData = SaveManager.Load();
            EnsureDefaults();
        }

        // --- Static pure calculation methods ---

        /// <summary>
        /// Calculates star rating (1-3) based on health remaining and time.
        /// 1 star: survived (healthPercent > 0)
        /// 2 stars: finished with >= 50% health
        /// 3 stars: finished with >= 50% health AND under par time
        /// </summary>
        public static int CalculateStars(float healthPercent, float timeSeconds, float parTime)
        {
            if (healthPercent <= 0f)
                return 0;

            int stars = 1;
            if (healthPercent >= 0.5f)
                stars = 2;
            if (healthPercent >= 0.5f && timeSeconds <= parTime)
                stars = 3;

            return stars;
        }

        /// <summary>
        /// Calculates currency reward based on star count.
        /// 1 star: 50, 2 stars: 100, 3 stars: 200.
        /// </summary>
        public static int CalculateCurrencyReward(int stars)
        {
            switch (stars)
            {
                case 1: return 50;
                case 2: return 100;
                case 3: return 200;
                default: return 0;
            }
        }

        /// <summary>
        /// Calculates bonus HP from health upgrade level. +20 per level.
        /// </summary>
        public static float CalculateHealthBonus(int upgradeLevel)
        {
            return upgradeLevel * 20f;
        }

        /// <summary>
        /// Calculates damage multiplier from damage upgrade level. +10% per level.
        /// </summary>
        public static float CalculateDamageMultiplier(int upgradeLevel)
        {
            return 1f + upgradeLevel * 0.1f;
        }

        /// <summary>
        /// Calculates armor multiplier (damage reduction) from armor upgrade level. -5% per level.
        /// </summary>
        public static float CalculateArmorMultiplier(int upgradeLevel)
        {
            return 1f - upgradeLevel * 0.05f;
        }
    }
}
