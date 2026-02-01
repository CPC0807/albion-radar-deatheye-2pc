using Albion.Network;
using VRise.Radar.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace VRise.Radar.Packets.Handlers
{
    public class NewHarvestableEvent : BaseEvent
    {
        byte[] offsets = Init.PacketOffsets.NewHarvestableObject;

        public NewHarvestableEvent(Dictionary<byte, object> parameters): base(parameters)
        {
            try
            {
                Id = parameters.ContainsKey(offsets[0]) ? Convert.ToInt32(parameters[offsets[0]]) : 0;

                Type = parameters.ContainsKey(offsets[1]) ? Convert.ToInt32(parameters[offsets[1]]) : 0;
                Tier = parameters.ContainsKey(offsets[2]) ? Convert.ToInt32(parameters[offsets[2]]) : 0;

                Position = parameters.ContainsKey(offsets[3]) && parameters[offsets[3]] is float[] pos
                    ? Additions.fromFArray(pos)
                    : Vector2.Zero;

                Count = parameters.ContainsKey(offsets[4]) ? Convert.ToInt32(parameters[offsets[4]]) : 0;
                Charge = parameters.ContainsKey(offsets[5]) ? Convert.ToInt32(parameters[offsets[5]]) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NewHarvestableEvent] Error parsing packet: {ex.Message}");
            }
        }

        public int Id { get; }

        public int Type { get; }
        public int Tier { get; }

        public Vector2 Position { get; }

        public int Count { get; }
        public int Charge { get; }
    }
}
