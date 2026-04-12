using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Weapon mastery tracking and cosmetic unlock persistence for PlayerRecord.
    /// </summary>
    public static partial class PlayerRecord
    {
        // --- Weapon Mastery ---

        public static int GetWeaponMasteryXP(string weaponId)
        {
            Load();
            return (int)_config.GetValue("mastery", weaponId, 0);
        }

        public static MasteryTier GetWeaponMasteryTier(string weaponId)
        {
            return WeaponMasteryState.GetTier(GetWeaponMasteryXP(weaponId));
        }

        public static void AwardWeaponMasteryXP(string weaponId, int amount)
        {
            if (amount <= 0 || string.IsNullOrEmpty(weaponId)) return;
            Load();
            int current = GetWeaponMasteryXP(weaponId);
            _config.SetValue("mastery", weaponId, current + amount);
            _config.Save(SavePath);
        }

        public static void ResetWeaponMastery(string weaponId)
        {
            Load();
            if (_config.HasSectionKey("mastery", weaponId))
            {
                _config.SetValue("mastery", weaponId, 0);
                _config.Save(SavePath);
            }
        }

        // --- Cosmetic Unlocks ---

        public static bool IsUnlocked(string unlockId)
        {
            Load();
            if (unlockId.StartsWith("hat_"))
                return ContainsId(_unlockedHats, unlockId);
            if (unlockId.StartsWith("emote_"))
                return ContainsId(_unlockedEmotes, unlockId);
            return false;
        }

        public static string[] GetUnlockedHats()
        {
            Load();
            return string.IsNullOrEmpty(_unlockedHats)
                ? new string[0]
                : _unlockedHats.Split(',');
        }

        public static string[] GetUnlockedEmotes()
        {
            Load();
            return string.IsNullOrEmpty(_unlockedEmotes)
                ? new string[0]
                : _unlockedEmotes.Split(',');
        }

        private static void PersistUnlock(string unlockId)
        {
            if (unlockId.StartsWith("hat_"))
            {
                if (!ContainsId(_unlockedHats, unlockId))
                {
                    _unlockedHats = string.IsNullOrEmpty(_unlockedHats)
                        ? unlockId
                        : _unlockedHats + "," + unlockId;
                }
            }
            else if (unlockId.StartsWith("emote_"))
            {
                if (!ContainsId(_unlockedEmotes, unlockId))
                {
                    _unlockedEmotes = string.IsNullOrEmpty(_unlockedEmotes)
                        ? unlockId
                        : _unlockedEmotes + "," + unlockId;
                }
            }
            Save();
        }

        private static bool ContainsId(string csv, string id)
        {
            if (string.IsNullOrEmpty(csv)) return false;
            var parts = csv.Split(',');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i] == id) return true;
            return false;
        }
    }
}
