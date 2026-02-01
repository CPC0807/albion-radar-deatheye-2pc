using Albion.Network;
using VRise.Radar.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace VRise.Radar.Packets.Handlers
{
    public class NewFishingZoneEvent : BaseEvent
    {
        byte[] offsets = Init.PacketOffsets.NewFishingZoneObject;

        public NewFishingZoneEvent(Dictionary<byte, object> parameters) : base(parameters)
        {
            try
            {
                Id = parameters.ContainsKey(offsets[0]) ? Convert.ToInt32(parameters[offsets[0]]) : 0;

                Position = parameters.ContainsKey(offsets[1]) && parameters[offsets[1]] is float[] pos
                    ? Additions.fromFArray(pos)
                    : Vector2.Zero;

                Size = parameters.ContainsKey(offsets[2]) ? Convert.ToInt32(parameters[offsets[2]]) : 0;
                RespawnCount = parameters.ContainsKey(offsets[3]) ? Convert.ToInt32(parameters[offsets[3]]) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NewFishingZoneEvent] Error parsing packet: {ex.Message}");
            }
        }

        public int Id { get; }

        public Vector2 Position { get; }

        public int Size { get; }
        public int RespawnCount { get; }
    }
}
