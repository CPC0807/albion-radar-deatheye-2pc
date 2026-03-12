using System.Numerics;
using System;
using System.Collections.Generic;
using VRise.Radar.Utility;
using VRise.Radar.GameObjects.Players;
using VRise.Protocol.Connect.Messages.ResponseObj;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;

namespace VRise.Radar.GameObjects.Mobs
{
    [Obfuscation(Feature = "mutation", Exclude = false)]
    public class MobsHandler
    {
        public ConcurrentDictionary<int, Mob> mobsList = new ConcurrentDictionary<int, Mob>();

        private readonly List<MobInfo> mobInfos;
        private readonly Dictionary<int, MobInfo> mobInfoByTypeId;

        public MobsHandler(List<MobInfo> mobInfos)
        {
            this.mobInfos = mobInfos;

            // 創建 typeId 到 MobInfo 的映射字典
            // typeId 是遊戲發送的 ID，MobInfo.Id 是 XML 索引
            mobInfoByTypeId = new Dictionary<int, MobInfo>();
            for (int i = 0; i < mobInfos.Count; i++)
            {
                // 將每個 MobInfo 以 typeId 為鍵存儲
                // typeId = XML索引（遊戲內部也是按這個順序）
                mobInfoByTypeId[i] = mobInfos[i];
            }
        }

        public void AddMob(int id, int typeId, Vector2 position, Health health, byte enchLvl)
        {
            lock (mobsList)
            {
                if (mobsList.ContainsKey(id))
                    mobsList.TryRemove(id, out Mob m);

                // 使用字典快速查找 typeId 對應的 MobInfo
                MobInfo foundMobInfo = null;
                if (mobInfoByTypeId.TryGetValue(typeId, out foundMobInfo))
                {
                    // 診斷日誌（可選）
                    #if DEBUG
                    Console.WriteLine($"[MobsHandler] Found mob: typeId={typeId}, Tier={foundMobInfo.Tier}, Type={foundMobInfo.Type}");
                    #endif
                }
                else
                {
                    // typeId 超出範圍，可能是新增的怪物或錯誤
                    Console.WriteLine($"[MobsHandler] WARNING: typeId {typeId} not found in mobInfos (total: {mobInfos.Count})");
                }

                mobsList.TryAdd(id, new Mob(id, typeId, position, enchLvl, foundMobInfo, health));
            }
        }

        public void UpdateMobPosition(int id, byte[] positionBytes, byte[] newPositionBytes, float speed, DateTime time)
        {
            var position = new Vector2(BitConverter.ToSingle(positionBytes, 4), BitConverter.ToSingle(positionBytes, 0));
            var newPosition = new Vector2(BitConverter.ToSingle(newPositionBytes, 4), BitConverter.ToSingle(newPositionBytes, 0));
            
            lock (mobsList)
            {
                if (mobsList.TryGetValue(id, out Mob mob))
                {
                    mob.Position = position;
                    mob.Speed = speed;
                    mob.Time = time;
                    mob.NewPosition = newPosition;
                }
            } 
        }

        public void SyncMobsPositions()
        {
            lock (mobsList)
            {
                foreach (Mob p in mobsList.Values.ToList())
                {
                    if (p == null || p.Speed == 0) continue;

                    Vector2 posDiff = p.Position - p.NewPosition;

                    if (posDiff == Vector2.Zero) continue;

                    p.Position -= posDiff * (float)((DateTime.UtcNow - p.Time).TotalSeconds / (posDiff.Magnitude() / (p.Speed / 10)));
                }
            }
        }

        public void Remove(int id)
        {
            lock (mobsList)
                mobsList.TryRemove(id, out Mob m);
        }

        public void Clear()
        {
            lock (mobsList)
                mobsList.Clear();
        }

        public void UpdateMobCharge(int mobId, int charge)
        {
            lock (mobsList)
            {
                if (mobsList.TryGetValue(mobId, out Mob mob))
                {
                    mob.Charge = charge;
                }
            }
                
        }

        public void UpdateHealth(int id, int health)
        {
            lock (mobsList)
            {
                if (mobsList.TryGetValue(id, out Mob mob))
                {
                    mob.Health.Value = health;
                }
            }
        }
    }
}
