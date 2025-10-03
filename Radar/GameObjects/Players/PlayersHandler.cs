using X975.Radar.Utility;
using System.Numerics;
using System;
using System.Collections.Generic;
using X975.Protocol.Connect.Messages.ResponseObj;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;

namespace X975.Radar.GameObjects.Players
{
    [Obfuscation(Feature = "mutation", Exclude = false)]
    public class PlayersHandler
    {
        public ConcurrentDictionary<int, Player> playersList = new ConcurrentDictionary<int, Player>();

        private readonly List<PlayerItems> itemsList = new List<PlayerItems>();

        public byte[] XorCode { get; set; }

        // 本地玩家座標（用於反推 XorCode）
        public static Vector2? LocalPlayerPosition { get; set; }

        // XorCode 生命週期追蹤（用於檢測時間規律）
        private DateTime? xorCodeRecoveredTime = null;
        private DateTime? xorCodeExpiredTime = null;
        private List<double> xorCodeLifetimes = new List<double>();

        // XorCode 樣本收集（用於反推 SessionKey）
        private static List<byte[]> xorCodeSamples = new List<byte[]>();
        private static bool sessionKeyAnalysisRun = false;

        public float[] Decrypt(byte[] coordinates, int offset = 0)
        {
            var code = XorCode;
            if (code == null)
            {
                return new[] { BitConverter.ToSingle(coordinates, offset), BitConverter.ToSingle(coordinates, offset + 4) };
            }

            var x = coordinates.Skip(offset).Take(4).ToArray();
            var y = coordinates.Skip(offset + 4).Take(4).ToArray();

            Decrypt(x, code, 0);
            Decrypt(y, code, 4);

            return new[] { BitConverter.ToSingle(x, 0), BitConverter.ToSingle(y, 0) };
        }

        private static void Decrypt(byte[] bytes4, byte[] saltBytes8, int saltPos)
        {
            for (var i = 0; i < bytes4.Length; i++)
            {
                var saltIndex = i % (saltBytes8.Length - saltPos) + saltPos;
                bytes4[i] ^= saltBytes8[saltIndex];
            }
        }

        public PlayersHandler(List<PlayerItems> itemsList)
        {
            this.itemsList = itemsList;
        }

        public void AddPlayer(int id, string name, string guild, string alliance, Vector2 position, Health health, Faction faction, int[] equipments, int[] spells)
        {
            lock (playersList)
            {
                if (playersList.ContainsKey(id))
                    playersList.TryRemove(id, out Player p);

                playersList.TryAdd(id, new Player(id, name, guild, alliance, position, health, faction, LoadEquipment(equipments), spells));

                // Debug: 輸出新玩家加入信息（可以在測試後移除）
                #if DEBUG
                Console.WriteLine($"[PlayerAdd] ID:{id} Name:{name} Guild:{guild} Pos:({position.X:F2},{position.Y:F2}) Faction:{faction}");
                #endif
            }
        }

        public void Remove(int id)
        {
            lock (playersList)
                playersList.TryRemove(id, out Player p);
        }

        public void Clear()
        {
            lock (playersList)
            {
                playersList.Clear();
                XorCode = null; // 重置 XorCode，因為換地圖時 XorCode 會改變
                #if DEBUG
                Console.WriteLine($"\n[Clear] PlayersList and XorCode reset (map change)");
                #endif
            }
        }

        public void Mounted(int id, bool IsMounted)
        {
            lock (playersList)
            {
                if (playersList.TryGetValue(id, out Player player))
                    player.IsMounted = IsMounted;
            }
        }

        public void UpdateHealth(int id, int health)
        {
            lock (playersList)
            {
                if (playersList.TryGetValue(id, out Player player))
                    player.Health.Value = health;
            }
        }

        public void SetFaction(int id, Faction faction)
        {
            lock (playersList)
            {
                if (playersList.TryGetValue(id, out Player player))
                    player.Faction = faction;
            }
        }

        public void RegenerateHealth()
        {
            lock (playersList)
            {
                foreach (Player p in playersList.Values.ToList())
                {
                    if (p != null && p.Health.IsRegeneration)
                        p.Health.Value += (int)p.Health.Regeneration;
                }
            }
        }

        public void UpdateItems(int id, int[] equipment, int[] spells)
        {
            lock (playersList)
            {
                if (playersList.TryGetValue(id, out Player player))
                {
                    player.Equipment = LoadEquipment(equipment);
                    player.Spells = spells;
                }
            }
        }

        public void SetRegeneration(int id, Health health)
        {
            lock (playersList)
            {
                if (playersList.TryGetValue(id, out Player player))
                    player.Health = health;
            }
        }

        public void SyncPlayersPosition()
        {
            lock (playersList)
            {
                foreach (Player p in playersList.Values.ToList())
                {
                    if (p == null || p.IsStanding || p.Speed == 0) continue;

                    Vector2 posDiff = p.Position - p.NewPosition;

                    if (posDiff == Vector2.Zero) continue;

                    p.Position -= posDiff * (float)((DateTime.UtcNow - p.Time).TotalSeconds / (posDiff.Magnitude() / (p.Speed / 10)));
                }
            } 
        }

        public void UpdatePlayerPosition(int id, byte[] positionBytes, byte[] newPositionBytes, float speed, DateTime time)
        {
            lock (playersList)
            {
                #if DEBUG
                string playerName = "Unknown";
                if (playersList.TryGetValue(id, out Player tempPlayer))
                {
                    playerName = tempPlayer.Name;
                }
                Console.WriteLine($"\n[UpdatePosition] ID:{id} Name:{playerName}");
                Console.WriteLine($"  RawBytes: {BitConverter.ToString(positionBytes)}");
                #endif

                // 嘗試暴力破解 XorCode（只在 XorCode 為 null 時執行一次）
                if (XorCode == null && LocalPlayerPosition.HasValue)
                {
                    #if DEBUG
                    Console.WriteLine($"  [BruteForce] Attempting to recover XorCode...");
                    Console.WriteLine($"  [MyPosition] ({LocalPlayerPosition.Value.X:F2}, {LocalPlayerPosition.Value.Y:F2})");

                    // 測試：如果這個玩家的 bytes 用我的座標解密會得到什麼？
                    Console.WriteLine($"  [Test] If other player is at MY position:");
                    byte[] testKey = XorCodeBruteForce.TryRecoverXorCode(positionBytes, LocalPlayerPosition.Value.X, LocalPlayerPosition.Value.Y);
                    if (testKey != null)
                    {
                        Console.WriteLine($"    XorCode: {BitConverter.ToString(testKey)}");
                        Vector2? testPos = XorCodeBruteForce.DecryptPosition(positionBytes, testKey);
                        if (testPos.HasValue)
                        {
                            Console.WriteLine($"    Verify: ({testPos.Value.X:F2}, {testPos.Value.Y:F2})");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"    Cannot recover - player is NOT at my position!");
                    }
                    #endif

                    // 使用本地玩家座標反推 XorCode
                    byte[] recoveredKey = XorCodeBruteForce.TryRecoverXorCode(positionBytes, LocalPlayerPosition.Value.X, LocalPlayerPosition.Value.Y);

                    if (recoveredKey != null)
                    {
                        XorCode = recoveredKey;
                        xorCodeRecoveredTime = DateTime.UtcNow;

                        // 收集 XorCode 樣本用於 SessionKey 分析
                        byte[] xorCodeCopy = new byte[8];
                        Array.Copy(XorCode, xorCodeCopy, 8);
                        xorCodeSamples.Add(xorCodeCopy);

                        #if DEBUG
                        Console.WriteLine($"  [SUCCESS!] XorCode recovered: {BitConverter.ToString(XorCode)}");
                        Console.WriteLine($"  [TIME] Recovered at: {xorCodeRecoveredTime.Value:HH:mm:ss.fff}");
                        Console.WriteLine($"  [SAMPLES] Collected {xorCodeSamples.Count} XorCode samples");

                        // 當收集到足夠樣本時，運行 SessionKey 分析
                        if (xorCodeSamples.Count >= 8 && !sessionKeyAnalysisRun)
                        {
                            sessionKeyAnalysisRun = true;
                            Console.WriteLine("\n" + new string('=', 60));
                            Console.WriteLine("  Enough samples collected! Running SessionKey analysis...");
                            Console.WriteLine(new string('=', 60));
                            //SessionKeyBruteForce.RunFullAnalysis();
                        }
                        #endif
                    }
                    else
                    {
                        #if DEBUG
                        Console.WriteLine($"  [FAILED] Player bytes do not match LocalPlayer position");
                        Console.WriteLine($"  [HINT] Players are at DIFFERENT positions! Ask them to stand exactly where you are!");
                        #endif
                    }
                }
                else if (XorCode == null)
                {
                    #if DEBUG
                    Console.WriteLine($"  [WAITING] LocalPlayer position not available yet. Move around first!");
                    #endif
                }

                // 智能解密：如果有 XorCode 就使用，否則直接解析
                Vector2 position, newPosition;

                if (XorCode != null)
                {
                    // 使用 XorCode 解密
                    Vector2? decPos = XorCodeBruteForce.DecryptPosition(positionBytes, XorCode);
                    Vector2? decNewPos = XorCodeBruteForce.DecryptPosition(newPositionBytes, XorCode);

                    position = decPos ?? Vector2.Zero;
                    newPosition = decNewPos ?? position;

                    #if DEBUG
                    Console.WriteLine($"  [Decrypted] ({position.X:F2}, {position.Y:F2})");
                    #endif

                    // 檢測 XorCode 是否失效（解密結果不合理）
                    if (decPos.HasValue)
                    {
                        // 更嚴格的失效檢測
                        bool isInvalid = false;

                        // 1. 極端值檢測
                        if (Math.Abs(position.X) > 10000f || Math.Abs(position.Y) > 10000f ||
                            float.IsNaN(position.X) || float.IsNaN(position.Y) ||
                            float.IsInfinity(position.X) || float.IsInfinity(position.Y))
                        {
                            isInvalid = true;
                        }

                        // 2. 與本地玩家距離檢測（如果有本地玩家位置）
                        if (!isInvalid && LocalPlayerPosition.HasValue)
                        {
                            float distanceToLocal = Vector2.Distance(position, LocalPlayerPosition.Value);

                            // 如果解密出的座標距離你太遠（超過 1000 單位），可能是錯誤
                            // 但這個閾值需要根據實際遊戲調整
                            if (distanceToLocal > 5000f)
                            {
                                #if DEBUG
                                Console.WriteLine($"  [WARNING] Decrypted position too far from LocalPlayer: {distanceToLocal:F2} units");
                                Console.WriteLine($"    LocalPos: ({LocalPlayerPosition.Value.X:F2}, {LocalPlayerPosition.Value.Y:F2})");
                                Console.WriteLine($"    Decrypted: ({position.X:F2}, {position.Y:F2})");
                                #endif
                                // 暫時不標記為失效，只警告
                                // isInvalid = true;
                            }
                        }

                        // 3. 檢測異常小的座標（朋友明明應該在你旁邊，卻解密出 (0, 7.98)）
                        if (!isInvalid && LocalPlayerPosition.HasValue)
                        {
                            // 如果本地玩家在 (33.94, 28.00)，但解密出 (0.00, 7.98)
                            // 距離差異 = sqrt((33.94-0)^2 + (28-7.98)^2) ≈ 39 單位
                            float distanceToLocal = Vector2.Distance(position, LocalPlayerPosition.Value);

                            // 如果朋友應該在你旁邊（用你的座標反推的 XorCode），但解密出來距離很遠
                            if (distanceToLocal > 10f)  // 超過 10 單位就懷疑
                            {
                                #if DEBUG
                                Console.WriteLine($"  [SUSPICIOUS] Decrypted position far from expected location: {distanceToLocal:F2} units away");
                                Console.WriteLine($"    Expected near: ({LocalPlayerPosition.Value.X:F2}, {LocalPlayerPosition.Value.Y:F2})");
                                Console.WriteLine($"    Got: ({position.X:F2}, {position.Y:F2})");
                                Console.WriteLine($"  [XorCode LIKELY CHANGED] Marking as expired");
                                #endif
                                isInvalid = true;
                            }
                        }

                        if (isInvalid)
                        {
                            xorCodeExpiredTime = DateTime.UtcNow;

                            #if DEBUG
                            Console.WriteLine($"  [XorCode EXPIRED] Decrypted position is invalid - resetting XorCode");

                            // 計算 XorCode 生命週期
                            if (xorCodeRecoveredTime.HasValue)
                            {
                                double lifetime = (xorCodeExpiredTime.Value - xorCodeRecoveredTime.Value).TotalSeconds;
                                xorCodeLifetimes.Add(lifetime);

                                Console.WriteLine($"  [LIFETIME] XorCode lasted: {lifetime:F2} seconds");
                                Console.WriteLine($"  [RECOVERED] at {xorCodeRecoveredTime.Value:HH:mm:ss.fff}");
                                Console.WriteLine($"  [EXPIRED]   at {xorCodeExpiredTime.Value:HH:mm:ss.fff}");

                                // 分析生命週期規律
                                if (xorCodeLifetimes.Count >= 3)
                                {
                                    double avgLifetime = xorCodeLifetimes.Average();
                                    double minLifetime = xorCodeLifetimes.Min();
                                    double maxLifetime = xorCodeLifetimes.Max();

                                    Console.WriteLine($"  [STATISTICS] Count: {xorCodeLifetimes.Count}");
                                    Console.WriteLine($"    Average: {avgLifetime:F2}s");
                                    Console.WriteLine($"    Min: {minLifetime:F2}s, Max: {maxLifetime:F2}s");
                                    Console.WriteLine($"    Variance: {maxLifetime - minLifetime:F2}s");

                                    // 檢測是否為固定週期
                                    if (maxLifetime - minLifetime < 5.0)
                                    {
                                        Console.WriteLine($"  [!!!] PATTERN DETECTED: XorCode changes every ~{avgLifetime:F0} seconds!");
                                        Console.WriteLine($"  [!!!] This suggests TIME-BASED XorCode generation!");
                                    }
                                }
                            }
                            #endif

                            XorCode = null;
                            position = Vector2.Zero;
                            newPosition = Vector2.Zero;
                        }
                    }
                }
                else
                {
                    // 直接解析（未加密模式）
                    position = new Vector2(BitConverter.ToSingle(positionBytes, 4), BitConverter.ToSingle(positionBytes, 0));
                    newPosition = new Vector2(BitConverter.ToSingle(newPositionBytes, 4), BitConverter.ToSingle(newPositionBytes, 0));

                    #if DEBUG
                    if (playersList.TryGetValue(id, out Player debugPlayer))
                    {
                        Console.WriteLine($"\n[Position Debug] ID:{id} Name:{debugPlayer.Name}");
                        Console.WriteLine($"  RawBytes: {BitConverter.ToString(positionBytes)}");
                        Console.WriteLine($"  Bytes[0-3]: {BitConverter.ToSingle(positionBytes, 0):F2}");
                        Console.WriteLine($"  Bytes[4-7]: {BitConverter.ToSingle(positionBytes, 4):F2}");
                        Console.WriteLine($"  [No XorCode] Position: ({position.X:F2}, {position.Y:F2})");
                    }
                    #endif
                }

                if (playersList.TryGetValue(id, out Player player))
                {
                    player.IsStanding = (player.Position - position).Magnitude() <= 0.05;
                    player.Position = position;
                    player.Speed = speed;
                    player.Time = time;
                    player.NewPosition = newPosition;
                }
            }
        }

        private Equipment LoadEquipment(int[] values)
        {
            Array.Resize(ref values, 8); //0-7

            Equipment equipment = new Equipment();

            for (int i = 0; i < values.Length; i++)
            {
                if (itemsList.Exists(x => x.Id == values[i]))
                {
                    equipment.Items.Add(itemsList.Find(x => x.Id == values[i]));
                }
                else if (values[i] == 0 || values[i] == -1)
                {
                    equipment.Items.Add(new PlayerItems() { Id = 0, Itempower = 0, Name = "NULL" });
                }
                else
                {
                    equipment.Items.Add(new PlayerItems() { Id = 0, Itempower = 0, Name = "T1_TRASH" });
                }
            }

            equipment.AllItemPower = GetItemPower(equipment.Items);

            return equipment.Items.All(x => x.Name == "T1_TRASH" || x.Name == "NULL") || equipment.AllItemPower == 0 ? null : equipment;
        }

        private int GetItemPower(List<PlayerItems> items)
        {
            if (items[0].Name.Contains("2H"))
                return items.FindAll(x => x != items[5] && x != items[7]).Sum(x => x.Itempower) / 5;

            return items.FindAll(x => x != items[5] && x != items[7]).Sum(x => x.Itempower) / 6;
        }
    }
}
