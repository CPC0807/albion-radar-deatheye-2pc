using Albion.Network;
using VRise.Radar.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace VRise.Radar.Packets.Handlers
{
    public class NewGatedWispEvent : BaseEvent
    {
        byte[] offsets = Init.PacketOffsets.NewWispGate;

        public NewGatedWispEvent(Dictionary<byte, object> parameters) : base(parameters)
        {
            try
            {
                Id = parameters.ContainsKey(offsets[0]) ? Convert.ToInt32(parameters[offsets[0]]) : 0;
                Position = parameters.ContainsKey(offsets[1]) && parameters[offsets[1]] is float[] pos
                    ? Additions.fromFArray(pos)
                    : Vector2.Zero;
                isCollected = parameters.ContainsKey(offsets[2]) && parameters[offsets[2]].ToString() == "2";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NewGatedWispEvent] Error parsing packet: {ex.Message}");
            }
        }

        public int Id { get; }

        public Vector2 Position { get; }

        public bool isCollected { get; }
    }
}
