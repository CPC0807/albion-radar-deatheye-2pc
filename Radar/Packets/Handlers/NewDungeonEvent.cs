using Albion.Network;
using VRise.Radar.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace VRise.Radar.Packets.Handlers
{
    public class NewDungeonEvent : BaseEvent
    {
        byte[] offsets = Init.PacketOffsets.NewDungeonExit;

        public NewDungeonEvent(Dictionary<byte, object> parameters) : base(parameters)
        {
            try
            {
                Id = parameters.ContainsKey(offsets[0]) ? Convert.ToInt32(parameters[offsets[0]]) : 0;

                Position = parameters.ContainsKey(offsets[1]) && parameters[offsets[1]] is float[] pos
                    ? Additions.fromFArray(pos)
                    : Vector2.Zero;

                Type = parameters.ContainsKey(offsets[2]) ? parameters[offsets[2]] as string : "NULL";

                Charges = parameters.ContainsKey(offsets[3]) ? Convert.ToInt32(parameters[offsets[3]]) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NewDungeonEvent] Error parsing packet: {ex.Message}");
            }
        }

        public int Id { get; }

        public Vector2 Position { get; }

        public string Type { get; }

        public int Charges { get; }
    }
}
