using System;
using System.Collections.Generic;
using System.Linq;
using VRise.Protocol.Connect.Messages.ResponseObj;

namespace VRise.Radar.Utility
{
    /// <summary>
    /// 動態檢測 Mob TypeId 的 Offset
    /// 用於自動適應 ao-bin-dumps 更新導致的索引變化
    /// </summary>
    public static class MobOffsetDetector
    {
        /// <summary>
        /// 錨點怪物列表
        /// Key: UniqueName (XML 中的唯一名稱，永遠不變)
        /// Value: 預期的 XML 索引（基於當前 ao-bin-dumps 版本）
        ///
        /// 選擇標準：
        /// 1. 遊戲核心怪物，不會被刪除
        /// 2. 從遊戲發布初期就存在
        /// 3. 分佈在不同索引區間（前、中、後）
        /// </summary>
        private static readonly Dictionary<string, int> AnchorMobs = new Dictionary<string, int>
        {
            // 索引 0 - 遊戲最早期的 Boss（T3）
            { "T3_MOB_TR_HERETIC_MAGE_BOSS", 0 },

            // 索引 3 - T4 召喚物
            { "T4_MOB_TR_HERETIC_SHADOWMASK_SUMMON", 3 },

            // 索引 9 - T4 Keeper Boss（常見）
            { "T4_MOB_TR_SILVER_KEEPER_EARTHDAUGHTER_BOSS", 9 },

            // 索引 402 - T5 寶藏熊（採集怪物）
            { "T5_MOB_TREASURE_BEAR", 402 },
        };

        /// <summary>
        /// 默認 Offset（如果檢測失敗時使用）
        /// </summary>
        private const int DEFAULT_OFFSET = 16;

        /// <summary>
        /// 檢測正確的 Offset
        /// </summary>
        /// <param name="mobInfos">從 XML 加載的 MobInfo 列表</param>
        /// <returns>檢測到的 Offset 值</returns>
        public static int DetectOffset(List<MobInfo> mobInfos)
        {
            Console.WriteLine("[MobOffsetDetector] Starting offset detection...");
            Console.WriteLine($"[MobOffsetDetector] Total mobs in XML: {mobInfos.Count}");

            // 用於統計檢測結果
            Dictionary<int, int> offsetVotes = new Dictionary<int, int>();

            // 遍歷所有錨點怪物
            foreach (var anchor in AnchorMobs)
            {
                string uniqueName = anchor.Key;
                int expectedIndex = anchor.Value;

                // 在 mobInfos 中查找這個怪物的實際索引
                int actualIndex = -1;
                for (int i = 0; i < mobInfos.Count; i++)
                {
                    if (mobInfos[i].UniqueName == uniqueName)
                    {
                        actualIndex = i;
                        break;
                    }
                }

                if (actualIndex >= 0)
                {
                    // 計算索引偏移（實際索引 - 預期索引）
                    int indexShift = actualIndex - expectedIndex;

                    Console.WriteLine($"[MobOffsetDetector] Anchor: {uniqueName}");
                    Console.WriteLine($"  Expected index: {expectedIndex}, Actual index: {actualIndex}, Shift: {indexShift}");

                    // 記錄這個偏移的投票
                    if (!offsetVotes.ContainsKey(indexShift))
                        offsetVotes[indexShift] = 0;
                    offsetVotes[indexShift]++;
                }
                else
                {
                    Console.WriteLine($"[MobOffsetDetector] WARNING: Anchor '{uniqueName}' not found in XML!");
                }
            }

            // 如果沒有找到任何錨點，使用默認值
            if (offsetVotes.Count == 0)
            {
                Console.WriteLine($"[MobOffsetDetector] No anchors found! Using default offset: {DEFAULT_OFFSET}");
                return DEFAULT_OFFSET;
            }

            // 找出得票最多的偏移量
            var mostVotedShift = offsetVotes.OrderByDescending(kv => kv.Value).First();
            int detectedIndexShift = mostVotedShift.Key;
            int voteCount = mostVotedShift.Value;

            Console.WriteLine($"[MobOffsetDetector] Most common index shift: {detectedIndexShift} (votes: {voteCount}/{AnchorMobs.Count})");

            // 計算最終的 Offset
            // Offset 的邏輯：遊戲 typeId = XML index + Offset
            // 所以 Offset 需要根據 index shift 調整
            int detectedOffset = DEFAULT_OFFSET - detectedIndexShift;

            Console.WriteLine($"[MobOffsetDetector] ========================================");
            Console.WriteLine($"[MobOffsetDetector] Detected Offset: {detectedOffset}");
            Console.WriteLine($"[MobOffsetDetector] (Previous default: {DEFAULT_OFFSET}, Index shift: {detectedIndexShift})");
            Console.WriteLine($"[MobOffsetDetector] ========================================");

            return detectedOffset;
        }

        /// <summary>
        /// 驗證檢測結果
        /// </summary>
        public static void VerifyOffset(List<MobInfo> mobInfos, int detectedOffset)
        {
            Console.WriteLine($"[MobOffsetDetector] Verifying offset {detectedOffset}...");

            foreach (var anchor in AnchorMobs)
            {
                string uniqueName = anchor.Key;
                int expectedIndex = anchor.Value;

                var mobInfo = mobInfos.FirstOrDefault(m => m.UniqueName == uniqueName);
                if (mobInfo != null)
                {
                    int actualIndex = mobInfo.Id;
                    int calculatedTypeId = actualIndex + detectedOffset;

                    Console.WriteLine($"  {uniqueName}:");
                    Console.WriteLine($"    XML Index: {actualIndex}, TypeId (with offset): {calculatedTypeId}");
                }
            }
        }
    }
}
