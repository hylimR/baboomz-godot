using Godot;
using Baboomz.Simulation;
using System;

namespace Baboomz
{
    /// <summary>
    /// Progression display for the match result screen: XP breakdown,
    /// rank bar, achievements, daily challenges, weapon mastery.
    /// </summary>
    public partial class MatchResultPanel
    {
        private void ShowProgression(GameState state)
        {
            if (state == null || state.Players.Length == 0) return;

            ref PlayerState p = ref state.Players[0];
            if (p.IsMob) return;

            // Build MatchStats for XP calculation
            var matchStats = new MatchStats
            {
                Won = state.WinnerIndex == 0,
                Draw = state.WinnerIndex < 0,
                TotalDamage = p.TotalDamageDealt,
                ShotsFired = p.ShotsFired,
                DirectHits = p.DirectHits,
                DamageTaken = p.TotalDamageTaken,
                MaxSingleDamage = p.MaxSingleDamage,
                LandedFirstBlood = state.FirstBloodPlayerIndex == 0
            };

            // Calculate XP
            var xpResult = RankSystem.CalculateMatchXP(matchStats);

            // Rank progression (before awarding XP)
            int oldXP = PlayerRecord.TotalXP;
            int oldRank = RankSystem.GetRankForXP(oldXP);
            string oldTitle = RankSystem.RankTitles[oldRank];

            // Award XP + check rank up
            var rankUp = PlayerRecord.AwardMatchXPAndCheckRankUp(matchStats);
            int newXP = PlayerRecord.TotalXP;
            int newRank = RankSystem.GetRankForXP(newXP);

            // Record win/loss/draw
            if (matchStats.Won) PlayerRecord.RecordWin();
            else if (matchStats.Draw) PlayerRecord.RecordDraw();
            else PlayerRecord.RecordLoss();

            // Build progression text
            string prog = "";

            // XP Summary
            prog += "--- XP Earned ---\n";
            prog += $"  Base: +{xpResult.BaseXP} XP\n";
            if (xpResult.Bonuses != null && xpResult.Bonuses.Length > 0)
            {
                for (int i = 0; i < xpResult.Bonuses.Length; i++)
                    prog += $"  {xpResult.Bonuses[i]}\n";
            }
            prog += $"  Total: +{xpResult.TotalXP} XP\n\n";

            // Rank
            prog += "--- Rank ---\n";
            prog += $"  {RankSystem.RankTitles[newRank]}";
            if (rankUp.NewRank > rankUp.OldRank)
                prog += $"  RANK UP!\n";
            else
                prog += "\n";
            int xpToNext = RankSystem.GetXPForNextRank(newXP);
            if (xpToNext > 0)
                prog += $"  {xpToNext} XP to next rank\n";
            else
                prog += "  MAX RANK\n";
            prog += "\n";

            // Rank-up unlocks
            if (rankUp.Unlocks != null && rankUp.Unlocks.Length > 0)
            {
                prog += "--- Unlocked! ---\n";
                for (int i = 0; i < rankUp.Unlocks.Length; i++)
                    prog += $"  {rankUp.Unlocks[i].DisplayName}\n";
                prog += "\n";
            }

            // Daily challenges
            var now = DateTime.Now;
            var dailyChallenges = ChallengeSystem.GetDailyChallenges(now.Year, now.Month, now.Day);
            var challengeStats = ChallengeSystem.BuildStats(state, 0);
            var challengeResults = ChallengeSystem.EvaluateChallenges(dailyChallenges, challengeStats);

            prog += "--- Daily Challenges ---\n";
            for (int i = 0; i < challengeResults.Length; i++)
            {
                string check = challengeResults[i].Completed ? "[x]" : "[ ]";
                prog += $"  {check} {challengeResults[i].ChallengeName}";
                if (challengeResults[i].Completed)
                    prog += $" +{challengeResults[i].XPReward} XP";
                prog += "\n";
            }
            prog += "\n";

            // Weapon mastery — award XP based on overall match performance.
            // Per-weapon shot tracking isn't in state, so use the active weapon
            // as the primary mastery beneficiary.
            prog += "--- Weapon Mastery ---\n";
            if (p.ShotsFired > 0)
            {
                string weaponId = p.WeaponSlots[p.ActiveWeaponSlot].WeaponId ?? "unknown";
                int masteryXP = WeaponMasteryCalc.Calculate(p.DirectHits, 0, true);
                if (masteryXP > 0)
                {
                    int currentXP = PlayerRecord.GetWeaponMasteryXP(weaponId);
                    var tier = WeaponMasteryState.GetTier(currentXP + masteryXP);
                    var oldTier = WeaponMasteryState.GetTier(currentXP);
                    prog += $"  {weaponId}: +{masteryXP} XP ({tier})";
                    if (tier > oldTier)
                        prog += " TIER UP!";
                    prog += "\n";
                    PlayerRecord.AwardWeaponMasteryXP(weaponId, masteryXP);
                }
            }
            else
            {
                prog += "  (no weapons used)\n";
            }

            _progressionLabel.Text = prog;

            // Update rank progress bar
            UpdateRankBar(newXP, newRank, rankUp.NewRank > rankUp.OldRank);
        }

        private void UpdateRankBar(int currentXP, int rank, bool rankedUp)
        {
            if (rank >= RankSystem.RankThresholds.Length - 1)
            {
                // Max rank — full bar
                _rankBarFill.AnchorRight = 1f;
                _rankLabel.Text = $"{RankSystem.RankTitles[rank]} — MAX RANK ({currentXP:N0} XP)";
            }
            else
            {
                int currentThreshold = RankSystem.RankThresholds[rank];
                int nextThreshold = RankSystem.RankThresholds[rank + 1];
                float progress = (float)(currentXP - currentThreshold) / (nextThreshold - currentThreshold);
                _rankBarFill.AnchorRight = Mathf.Clamp(progress, 0f, 1f);
                _rankLabel.Text = $"{RankSystem.RankTitles[rank]} — {currentXP:N0} / {nextThreshold:N0} XP";
            }

            if (rankedUp)
            {
                _rankBarFill.Color = new Color(1f, 0.84f, 0f); // bright gold
                _rankLabel.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0f));
            }
        }
    }
}
