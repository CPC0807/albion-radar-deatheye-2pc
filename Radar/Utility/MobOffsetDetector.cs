using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRise.Protocol.Connect.Messages.ResponseObj;

namespace VRise.Radar.Utility
{
    public sealed class AnchorMob
    {
        public AnchorMob(string uniqueName, int expectedIndex, string description)
        {
            UniqueName = uniqueName;
            ExpectedIndex = expectedIndex;
            Description = description;
        }

        public string UniqueName { get; }
        public int ExpectedIndex { get; }
        public string Description { get; }
    }

    public sealed class AnchorMatch
    {
        public AnchorMatch(AnchorMob anchor, int actualIndex)
        {
            Anchor = anchor;
            ActualIndex = actualIndex;
        }

        public AnchorMob Anchor { get; }
        public int ActualIndex { get; }
        public int IndexShift => ActualIndex - Anchor.ExpectedIndex;
    }

    public sealed class MobOffsetDetectionResult
    {
        public bool Success { get; set; }
        public int? DetectedOffset { get; set; }
        public int MatchedAnchors { get; set; }
        public int TotalAnchors { get; set; }
        public int? WinningShift { get; set; }
        public IReadOnlyDictionary<int, int> VoteBreakdown { get; set; }
        public string FailureReason { get; set; }
        public IReadOnlyList<AnchorMatch> Matches { get; set; }

        public string FormatSummary()
        {
            string votes = VoteBreakdown == null || VoteBreakdown.Count == 0
                ? "none"
                : string.Join(", ", VoteBreakdown.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}:{kv.Value}"));

            return $"success={Success}, matched={MatchedAnchors}/{TotalAnchors}, winningShift={WinningShift?.ToString() ?? "n/a"}, detectedOffset={DetectedOffset?.ToString() ?? "n/a"}, votes=[{votes}]";
        }
    }

    /// <summary>
    /// Dynamically detects the mob typeId offset from mobs.xml and rejects low-confidence results.
    /// </summary>
    public static class MobOffsetDetector
    {
        private const int DefaultOffset = 16;
        private const int MinimumMatchedAnchors = 4;
        private const double RequiredWinningVoteRatio = 0.70;
        private const int MinimumAllowedOffset = 1;
        private const int MaximumAllowedOffset = 128;

        private static readonly IReadOnlyList<AnchorMob> AnchorMobs = new[]
        {
            new AnchorMob("T3_MOB_TR_HERETIC_MAGE_BOSS", 0, "Early tracking boss"),
            new AnchorMob("T4_MOB_TR_HERETIC_SHADOWMASK_SUMMON", 3, "Early summon"),
            new AnchorMob("T4_MOB_TR_SILVER_KEEPER_EARTHDAUGHTER_BOSS", 9, "Early keeper boss"),
            new AnchorMob("T5_MOB_TREASURE_BEAR", 402, "Early harvestable mob"),
            new AnchorMob("T7_MOB_ROAMING_HERETIC_THIEF", 1000, "Mid roaming mob"),
            new AnchorMob("T8_MOB_UNDEAD_SPECIAL_PULLER_VETERAN", 2000, "Late-mid veteran mob"),
            new AnchorMob("T5_MOB_MD_UNDEAD_SKELETON_MELEE", 3000, "Dungeon mob"),
            new AnchorMob("T5_MOB_RD_MORGANA_GIANT", 4000, "Roads mob"),
        };

        public static MobOffsetDetectionResult DetectOffset(List<MobInfo> mobInfos)
        {
            Console.WriteLine("[MobOffsetDetector] Starting offset detection...");
            Console.WriteLine($"[MobOffsetDetector] Total mobs in XML: {mobInfos.Count}");

            var matches = new List<AnchorMatch>();
            var votes = new Dictionary<int, int>();
            var mobIndexByUniqueName = mobInfos
                .Where(m => !string.IsNullOrEmpty(m.UniqueName))
                .GroupBy(m => m.UniqueName)
                .ToDictionary(g => g.Key, g => g.First().Id);

            foreach (var anchor in AnchorMobs)
            {
                if (!mobIndexByUniqueName.TryGetValue(anchor.UniqueName, out int actualIndex))
                {
                    Console.WriteLine($"[MobOffsetDetector] WARNING: Anchor '{anchor.UniqueName}' ({anchor.Description}) not found in XML!");
                    continue;
                }

                var match = new AnchorMatch(anchor, actualIndex);
                matches.Add(match);

                if (!votes.ContainsKey(match.IndexShift))
                    votes[match.IndexShift] = 0;
                votes[match.IndexShift]++;

                Console.WriteLine($"[MobOffsetDetector] Anchor: {anchor.UniqueName} ({anchor.Description})");
                Console.WriteLine($"  Expected index: {anchor.ExpectedIndex}, Actual index: {actualIndex}, Shift: {match.IndexShift}");
            }

            var result = new MobOffsetDetectionResult
            {
                MatchedAnchors = matches.Count,
                TotalAnchors = AnchorMobs.Count,
                VoteBreakdown = new Dictionary<int, int>(votes),
                Matches = matches,
            };

            if (matches.Count < MinimumMatchedAnchors)
            {
                result.FailureReason = $"Only matched {matches.Count}/{AnchorMobs.Count} anchors; need at least {MinimumMatchedAnchors}.";
                LogFailure(result);
                return result;
            }

            if (votes.Count == 0)
            {
                result.FailureReason = "No valid anchor votes were collected.";
                LogFailure(result);
                return result;
            }

            var orderedVotes = votes.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).ToList();
            var winningVote = orderedVotes[0];
            result.WinningShift = winningVote.Key;

            if (orderedVotes.Count > 1 && orderedVotes[1].Value == winningVote.Value)
            {
                result.FailureReason = $"Anchor votes are tied between shifts {winningVote.Key} and {orderedVotes[1].Key}.";
                LogFailure(result);
                return result;
            }

            double winningRatio = (double)winningVote.Value / matches.Count;
            if (winningRatio < RequiredWinningVoteRatio)
            {
                result.FailureReason = $"Winning shift {winningVote.Key} only has {winningVote.Value}/{matches.Count} votes ({winningRatio:P0}); need at least {RequiredWinningVoteRatio:P0}.";
                LogFailure(result);
                return result;
            }

            int detectedOffset = DefaultOffset - winningVote.Key;
            result.DetectedOffset = detectedOffset;

            if (detectedOffset < MinimumAllowedOffset || detectedOffset > MaximumAllowedOffset)
            {
                result.FailureReason = $"Detected offset {detectedOffset} is outside the allowed range {MinimumAllowedOffset}..{MaximumAllowedOffset}.";
                LogFailure(result);
                return result;
            }

            result.Success = true;

            Console.WriteLine($"[MobOffsetDetector] Most common index shift: {winningVote.Key} (votes: {winningVote.Value}/{matches.Count})");
            Console.WriteLine("[MobOffsetDetector] ========================================");
            Console.WriteLine($"[MobOffsetDetector] Detected Offset: {detectedOffset}");
            Console.WriteLine($"[MobOffsetDetector] (Default offset: {DefaultOffset}, Index shift: {winningVote.Key})");
            Console.WriteLine($"[MobOffsetDetector] Summary: {result.FormatSummary()}");
            Console.WriteLine("[MobOffsetDetector] ========================================");

            return result;
        }

        public static bool VerifyOffset(List<MobInfo> mobInfos, MobOffsetDetectionResult detectionResult)
        {
            if (detectionResult == null)
            {
                Console.WriteLine("[MobOffsetDetector] Verification skipped because detection result is null.");
                return false;
            }

            if (!detectionResult.Success || !detectionResult.DetectedOffset.HasValue)
            {
                Console.WriteLine($"[MobOffsetDetector] Verification skipped because detection failed: {detectionResult.FailureReason}");
                return false;
            }

            int detectedOffset = detectionResult.DetectedOffset.Value;
            Console.WriteLine($"[MobOffsetDetector] Verifying offset {detectedOffset}...");

            var mobInfoByUniqueName = mobInfos
                .Where(m => !string.IsNullOrEmpty(m.UniqueName))
                .GroupBy(m => m.UniqueName)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var match in detectionResult.Matches.OrderBy(m => m.Anchor.ExpectedIndex))
            {
                if (!mobInfoByUniqueName.TryGetValue(match.Anchor.UniqueName, out var mobInfo))
                {
                    Console.WriteLine($"[MobOffsetDetector] Verification failed: anchor '{match.Anchor.UniqueName}' disappeared.");
                    return false;
                }

                int actualIndex = mobInfo.Id;
                if (actualIndex != match.ActualIndex)
                {
                    Console.WriteLine($"[MobOffsetDetector] Verification failed: anchor '{match.Anchor.UniqueName}' moved from recorded index {match.ActualIndex} to {actualIndex}.");
                    return false;
                }

                int calculatedTypeId = actualIndex + detectedOffset;
                Console.WriteLine($"  {match.Anchor.UniqueName}:");
                Console.WriteLine($"    XML Index: {actualIndex}, TypeId (with offset): {calculatedTypeId}");
            }

            Console.WriteLine("[MobOffsetDetector] Verification passed.");
            return true;
        }

        public static string BuildFailureMessage(MobOffsetDetectionResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Mob offset detection failed.");
            builder.AppendLine(result?.FailureReason ?? "Unknown reason.");

            if (result != null)
            {
                builder.AppendLine(result.FormatSummary());
            }

            builder.AppendLine("Update ao-bin-dumps or review the anchor definitions before starting the radar.");
            return builder.ToString();
        }

        private static void LogFailure(MobOffsetDetectionResult result)
        {
            Console.WriteLine("[MobOffsetDetector] ========================================");
            Console.WriteLine($"[MobOffsetDetector] Detection failed: {result.FailureReason}");
            Console.WriteLine($"[MobOffsetDetector] Summary: {result.FormatSummary()}");
            Console.WriteLine("[MobOffsetDetector] ========================================");
        }
    }
}
