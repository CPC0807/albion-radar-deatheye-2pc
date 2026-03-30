using Albion.Network;
using VRise.Radar.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using VRise.Radar.GameObjects.Players;
using System.Reflection;

namespace VRise.Radar.Packets.Handlers
{
    class NewMobEvent : BaseEvent
    {
        byte[] offsets = Init.PacketOffsets.NewMobEvent;

        public NewMobEvent(Dictionary<byte, object> parameters) : base(parameters)
        {
            try
            {
                Id = parameters.ContainsKey(offsets[0]) ? Convert.ToInt32(parameters[offsets[0]]) : 0;

                // 使用動態檢測的 Offset（自動適應 ao-bin-dumps 更新）
                RawTypeId = parameters.ContainsKey(offsets[1]) ? Convert.ToInt32(parameters[offsets[1]]) : 0;
                TypeId = RawTypeId - Init.MobTypeIdOffset;

                // 診斷日誌（Debug 模式下顯示）
                #if DEBUG
                if (RawTypeId > 0)
                {
                    Console.WriteLine($"[NewMobEvent] Raw typeId: {RawTypeId}, Offset: {Init.MobTypeIdOffset}, Final TypeId: {TypeId}");
                }
                #endif

                Position = parameters.ContainsKey(offsets[2]) && parameters[offsets[2]] is float[] pos
                    ? Additions.fromFArray(pos)
                    : Vector2.Zero;

                if (parameters.ContainsKey(offsets[4]))
                {
                    Health = parameters.ContainsKey(offsets[3])
                        ? new Health(Convert.ToInt32(parameters[offsets[3]]), Convert.ToInt32(parameters[offsets[4]]))
                        : new Health(Convert.ToInt32(parameters[offsets[4]]));
                }
                else
                {
                    Health = new Health(100);
                }

                Charge = (byte)(parameters.ContainsKey(offsets[5]) ? Convert.ToInt32(parameters[offsets[5]]) : 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NewMobEvent] Error parsing packet: {ex.Message}");
                Health = new Health(100);
            }
        }

        public int Id { get; }

        public int RawTypeId { get; }
        public int TypeId { get; }
        public Vector2 Position { get; }

        public Health Health { get; }

        public byte Charge { get; }
    }
}
