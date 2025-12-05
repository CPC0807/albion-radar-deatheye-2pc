using VRise.Radar.Utility;
using System.Numerics;
using System;
using System.Collections.Generic;
using VRise.Protocol.Connect.Messages.ResponseObj;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;

namespace VRise.Radar.GameObjects.Players
{
    [Obfuscation(Feature = "mutation", Exclude = false)]
    public class PlayersHandler
    {
        public ConcurrentDictionary<int, Player> playersList = new ConcurrentDictionary<int, Player>();

        private readonly List<PlayerItems> itemsList = new List<PlayerItems>();

        public byte[] XorCode { get; set; }

        public float[] Decrypt(byte[] coordinates, int offset = 0)
        {
            // 驗證輸入
            if (coordinates == null)
            {
                #if DEBUG
                Console.WriteLine($"[Decrypt] ERROR: coordinates is NULL!");
                #endif
                return new float[] { 0f, 0f };
            }

            if (coordinates.Length < offset + 8)
            {
                #if DEBUG
                Console.WriteLine($"[Decrypt] ERROR: coordinates length {coordinates.Length} is too short for offset {offset}!");
                #endif
                return new float[] { 0f, 0f };
            }

            var code = XorCode;
            if (code == null)
            {
                // 如果沒有 XorCode，嘗試直接解析（可能座標未加密）
                try
                {
                    return new[] { BitConverter.ToSingle(coordinates, offset), BitConverter.ToSingle(coordinates, offset + 4) };
                }
                catch (Exception ex)
                {
                    #if DEBUG
                    Console.WriteLine($"[Decrypt] ERROR parsing unencrypted coordinates: {ex.Message}");
                    #endif
                    return new float[] { 0f, 0f };
                }
            }

            try
            {
                var x = coordinates.Skip(offset).Take(4).ToArray();
                var y = coordinates.Skip(offset + 4).Take(4).ToArray();

                if (x.Length != 4 || y.Length != 4)
                {
                    #if DEBUG
                    Console.WriteLine($"[Decrypt] ERROR: Invalid array lengths after Skip/Take: x={x.Length}, y={y.Length}");
                    #endif
                    return new float[] { 0f, 0f };
                }

                Decrypt(x, code, 0);
                Decrypt(y, code, 4);

                return new[] { BitConverter.ToSingle(x, 0), BitConverter.ToSingle(y, 0) };
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"[Decrypt] ERROR: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"[Decrypt] Stack trace: {ex.StackTrace}");
                #endif
                return new float[] { 0f, 0f };
            }
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
                playersList.Clear();
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
                // Debug: 診斷參數類型
                if (positionBytes == null)
                {
                    Console.WriteLine($"[UpdatePlayerPosition] ERROR: positionBytes is NULL!");
                    return;
                }
                if (newPositionBytes == null)
                {
                    Console.WriteLine($"[UpdatePlayerPosition] ERROR: newPositionBytes is NULL!");
                    return;
                }
                if (positionBytes.Length != 8)
                {
                    Console.WriteLine($"[UpdatePlayerPosition] WARNING: positionBytes length is {positionBytes.Length} (expected 8)");
                }
                if (newPositionBytes.Length != 8)
                {
                    Console.WriteLine($"[UpdatePlayerPosition] WARNING: newPositionBytes length is {newPositionBytes.Length} (expected 8)");
                }

                // Debug: 診斷 XorCode 狀態
                if (XorCode == null)
                {
                    Console.WriteLine($"[XorCode] WARNING: XorCode is NULL! Cannot decrypt positions.");
                    Console.WriteLine($"[XorCode] Make sure KeySync event (ID 593) is triggered when entering a new zone.");
                }
                else if (XorCode.Length != 8)
                {
                    Console.WriteLine($"[XorCode] WARNING: Invalid XorCode length: {XorCode.Length} (expected 8)");
                }
                #endif

                // 使用 Decrypt 方法解密座標
                float[] pos;
                float[] newPos;

                try
                {
                    pos = Decrypt(positionBytes);
                    newPos = Decrypt(newPositionBytes);
                }
                catch (InvalidCastException ex)
                {
                    #if DEBUG
                    Console.WriteLine($"[UpdatePlayerPosition] InvalidCastException: {ex.Message}");
                    Console.WriteLine($"[UpdatePlayerPosition] positionBytes type: {positionBytes?.GetType().Name ?? "null"}");
                    Console.WriteLine($"[UpdatePlayerPosition] newPositionBytes type: {newPositionBytes?.GetType().Name ?? "null"}");
                    #endif
                    return;
                }
                catch (Exception ex)
                {
                    #if DEBUG
                    Console.WriteLine($"[UpdatePlayerPosition] Exception in Decrypt: {ex.GetType().Name} - {ex.Message}");
                    #endif
                    return;
                }

                Vector2 position = new Vector2(pos[0], pos[1]);
                Vector2 newPosition = new Vector2(newPos[0], newPos[1]);

                #if DEBUG
                // 檢測異常的座標值（可能表示解密失敗）
                bool isAbnormal = false;
                if (Math.Abs(position.X) > 100000 || Math.Abs(position.Y) > 100000)
                {
                    isAbnormal = true;
                    Console.WriteLine($"\n========== [ABNORMAL POSITION DETECTED!] ==========");
                    Console.WriteLine($"  Player ID: {id}");
                    Console.WriteLine($"  Decrypted Position: ({position.X:F2}, {position.Y:F2})");
                    Console.WriteLine($"  Raw positionBytes: {BitConverter.ToString(positionBytes)}");
                    Console.WriteLine($"  XorCode: {(XorCode != null ? BitConverter.ToString(XorCode) : "NULL")}");
                    Console.WriteLine($"  XorCode Length: {XorCode?.Length ?? 0}");

                    // 嘗試不解密直接讀取
                    float directX = BitConverter.ToSingle(positionBytes, 0);
                    float directY = BitConverter.ToSingle(positionBytes, 4);
                    Console.WriteLine($"  Direct read (X at 0, Y at 4): ({directX:F2}, {directY:F2})");

                    // 嘗試反向讀取
                    float reverseX = BitConverter.ToSingle(positionBytes, 4);
                    float reverseY = BitConverter.ToSingle(positionBytes, 0);
                    Console.WriteLine($"  Reverse read (X at 4, Y at 0): ({reverseX:F2}, {reverseY:F2})");
                    Console.WriteLine($"==================================================\n");
                }
                #endif

                if (playersList.TryGetValue(id, out Player player))
                {
                    player.IsStanding = (player.Position - position).Magnitude() <= 0.05;
                    player.Position = position;
                    player.Speed = speed;
                    player.Time = time;
                    player.NewPosition = newPosition;

                    #if DEBUG
                    // Debug: 只輸出正常的位置信息
                    if (!isAbnormal)
                    {
                        Console.WriteLine($"[PlayerPos] ID:{id} Name:{player.Name} Pos:({position.X:F2},{position.Y:F2}) NewPos:({newPosition.X:F2},{newPosition.Y:F2}) Speed:{speed:F2}");
                    }

                    // 如果位置是 (0.00, 0.00)，輸出原始數據
                    if (position.X == 0.0f && position.Y == 0.0f && !isAbnormal)
                    {
                        Console.WriteLine($"[DEBUG] Position is (0,0) - Raw bytes: {BitConverter.ToString(positionBytes)}");
                        Console.WriteLine($"[DEBUG] XorCode: {(XorCode != null ? BitConverter.ToString(XorCode) : "NULL")}");
                    }
                    #endif
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
