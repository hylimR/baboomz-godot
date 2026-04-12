using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Persists player win/loss/draw record, XP/rank, weapon mastery,
    /// skill loadout, unlocked cosmetics, and survival high scores
    /// across sessions via Godot ConfigFile.
    /// Replaces Unity's PlayerPrefs-based PlayerRecord.
    /// </summary>
    public static partial class PlayerRecord
    {
        private const string SavePath = "user://player_record.cfg";
        private static ConfigFile _config = new();
        private static bool _loaded;

        // --- Core Record ---

        public static int Wins { get; private set; }
        public static int Losses { get; private set; }
        public static int Draws { get; private set; }
        public static int GamesPlayed { get; private set; }
        public static int TotalXP { get; private set; }

        // --- Survival ---

        public static int SurvivalHighScore { get; private set; }
        public static int SurvivalHighWave { get; private set; }

        // --- Skill Loadout ---

        private static int _lastSkill0;
        private static int _lastSkill1;

        // --- Unlocked Cosmetics ---

        private static string _unlockedHats = "";
        private static string _unlockedEmotes = "";

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            if (_config.Load(SavePath) != Error.Ok) return;

            Wins = (int)_config.GetValue("record", "wins", 0);
            Losses = (int)_config.GetValue("record", "losses", 0);
            Draws = (int)_config.GetValue("record", "draws", 0);
            GamesPlayed = (int)_config.GetValue("record", "games_played", 0);
            TotalXP = (int)_config.GetValue("record", "total_xp", 0);

            SurvivalHighScore = (int)_config.GetValue("survival", "high_score", 0);
            SurvivalHighWave = (int)_config.GetValue("survival", "high_wave", 0);

            _lastSkill0 = (int)_config.GetValue("loadout", "skill0", 0);
            _lastSkill1 = (int)_config.GetValue("loadout", "skill1", 3);

            _unlockedHats = (string)_config.GetValue("unlocks", "hats", "");
            _unlockedEmotes = (string)_config.GetValue("unlocks", "emotes", "");
        }

        public static void RecordWin()
        {
            Load();
            Wins++;
            GamesPlayed++;
            Save();
        }

        public static void RecordLoss()
        {
            Load();
            Losses++;
            GamesPlayed++;
            Save();
        }

        public static void RecordDraw()
        {
            Load();
            Draws++;
            GamesPlayed++;
            Save();
        }

        public static void AwardXP(int amount)
        {
            if (amount <= 0) return;
            Load();
            TotalXP += amount;
            Save();
        }

        public static MatchXPResult CalculateMatchXP(MatchStats stats)
        {
            return RankSystem.CalculateMatchXP(stats);
        }

        public static int GetRankIndex()
        {
            Load();
            return RankSystem.GetRankForXP(TotalXP);
        }

        public static string GetRankTitle()
        {
            Load();
            return RankSystem.GetRankTitle(TotalXP);
        }

        public static string GetRecordString()
        {
            Load();
            int tier = UnlockRegistry.GetTier(Wins);
            string tierName = UnlockRegistry.GetTierName(tier);
            int winsNeeded = UnlockRegistry.GetWinsForNextTier(Wins);
            string progress = winsNeeded > 0 ? $" ({winsNeeded} wins to next)" : " (MAX)";
            string survival = SurvivalHighScore > 0
                ? $" | Best: Wave {SurvivalHighWave} \u2014 {SurvivalHighScore:N0} pts"
                : "";
            return $"{GetRankTitle()} [{tierName}]{progress} \u2014 W:{Wins} L:{Losses} D:{Draws} ({TotalXP:N0} XP){survival}";
        }

        // --- Skill Loadout ---

        public static void SaveLastLoadout(int skill0, int skill1)
        {
            Load();
            _lastSkill0 = skill0;
            _lastSkill1 = skill1;
            Save();
        }

        public static void LoadLastLoadout()
        {
            Load();
            GameModeContext.SelectedSkillSlot0 = _lastSkill0;
            GameModeContext.SelectedSkillSlot1 = _lastSkill1;
        }

        // --- XP + Rank Up ---

        public static RankUpResult AwardMatchXPAndCheckRankUp(MatchStats stats)
        {
            Load();
            int oldXP = TotalXP;
            var xpResult = RankSystem.CalculateMatchXP(stats);
            AwardXP(xpResult.TotalXP);
            var rankUp = RankSystem.CheckRankUp(oldXP, TotalXP);
            for (int i = 0; i < rankUp.Unlocks.Length; i++)
                PersistUnlock(rankUp.Unlocks[i].UnlockId);
            return rankUp;
        }

        // --- Survival High Score ---

        /// <summary>
        /// Updates survival high score/wave if the new run exceeds the previous best.
        /// Returns true if a new personal best was set.
        /// </summary>
        public static bool TryUpdateSurvivalHighScore(int score, int wave)
        {
            Load();
            bool newBest = score > SurvivalHighScore;
            if (newBest)
            {
                SurvivalHighScore = score;
                SurvivalHighWave = wave;
                Save();
            }
            return newBest;
        }

        // --- Reset ---

        public static void Reset()
        {
            Wins = 0;
            Losses = 0;
            Draws = 0;
            GamesPlayed = 0;
            TotalXP = 0;
            SurvivalHighScore = 0;
            SurvivalHighWave = 0;
            _lastSkill0 = 0;
            _lastSkill1 = 3;
            _unlockedHats = "";
            _unlockedEmotes = "";
            _config = new ConfigFile();
            Save();
        }

        private static void Save()
        {
            _config.SetValue("record", "wins", Wins);
            _config.SetValue("record", "losses", Losses);
            _config.SetValue("record", "draws", Draws);
            _config.SetValue("record", "games_played", GamesPlayed);
            _config.SetValue("record", "total_xp", TotalXP);

            _config.SetValue("survival", "high_score", SurvivalHighScore);
            _config.SetValue("survival", "high_wave", SurvivalHighWave);

            _config.SetValue("loadout", "skill0", _lastSkill0);
            _config.SetValue("loadout", "skill1", _lastSkill1);

            _config.SetValue("unlocks", "hats", _unlockedHats);
            _config.SetValue("unlocks", "emotes", _unlockedEmotes);

            _config.Save(SavePath);
        }
    }
}
