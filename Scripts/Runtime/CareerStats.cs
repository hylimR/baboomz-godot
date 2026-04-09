using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Persists lifetime career statistics via Godot ConfigFile.
    /// Updated at match end from GameState player fields.
    /// Displayed on the main menu as a compact one-liner.
    /// Replaces Unity's PlayerPrefs-based CareerStats.
    /// </summary>
    public static class CareerStats
    {
        private const string SavePath = "user://career_stats.cfg";
        private static ConfigFile _config = new();
        private static bool _loaded;

        public static int TotalKills { get; private set; }
        public static int TotalDeaths { get; private set; }
        public static float TotalDamageDealt { get; private set; }
        public static int TotalShotsFired { get; private set; }
        public static int TotalShotsHit { get; private set; }
        public static float BestSingleHit { get; private set; }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            if (_config.Load(SavePath) != Error.Ok) return;

            TotalKills = (int)_config.GetValue("stats", "kills", 0);
            TotalDeaths = (int)_config.GetValue("stats", "deaths", 0);
            TotalDamageDealt = (float)_config.GetValue("stats", "damage", 0f);
            TotalShotsFired = (int)_config.GetValue("stats", "shots", 0);
            TotalShotsHit = (int)_config.GetValue("stats", "hits", 0);
            BestSingleHit = (float)_config.GetValue("stats", "best_hit", 0f);
        }

        /// <summary>
        /// Accumulates career stats from the local player's match results.
        /// Call once per match end, after the match is concluded.
        /// </summary>
        public static void AccumulateFromMatch(ref PlayerState localPlayer)
        {
            Load();

            TotalKills += localPlayer.TotalKills;
            TotalDeaths += localPlayer.IsDead ? 1 : 0;
            TotalDamageDealt += localPlayer.TotalDamageDealt;
            TotalShotsFired += localPlayer.ShotsFired;
            TotalShotsHit += localPlayer.DirectHits;

            if (localPlayer.MaxSingleDamage > BestSingleHit)
                BestSingleHit = localPlayer.MaxSingleDamage;

            Save();
        }

        /// <summary>
        /// Returns accuracy as an integer percentage (0-100), or 0 if no shots fired.
        /// </summary>
        public static int AccuracyPct()
        {
            Load();
            if (TotalShotsFired == 0) return 0;
            return Mathf.RoundToInt((float)TotalShotsHit / TotalShotsFired * 100f);
        }

        /// <summary>
        /// Returns a compact one-liner for main menu display.
        /// Example: "42 kills - 61% acc - best hit 94"
        /// </summary>
        public static string GetSummaryLine()
        {
            Load();
            if (TotalShotsFired == 0) return "";
            string bestHit = BestSingleHit > 0f ? $" \u00b7 best hit {BestSingleHit:0}" : "";
            return $"{TotalKills} kills \u00b7 {AccuracyPct()}% acc{bestHit}";
        }

        /// <summary>Resets all career stats (for testing / reset flow).</summary>
        public static void Reset()
        {
            TotalKills = 0;
            TotalDeaths = 0;
            TotalDamageDealt = 0f;
            TotalShotsFired = 0;
            TotalShotsHit = 0;
            BestSingleHit = 0f;
            _config = new ConfigFile();
            Save();
        }

        private static void Save()
        {
            _config.SetValue("stats", "kills", TotalKills);
            _config.SetValue("stats", "deaths", TotalDeaths);
            _config.SetValue("stats", "damage", TotalDamageDealt);
            _config.SetValue("stats", "shots", TotalShotsFired);
            _config.SetValue("stats", "hits", TotalShotsHit);
            _config.SetValue("stats", "best_hit", BestSingleHit);
            _config.Save(SavePath);
        }
    }
}
