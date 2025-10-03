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
            Id = Convert.ToInt32(parameters[offsets[0]]);
            Position = Additions.fromFArray((float[])parameters[offsets[1]]);
            isCollected = parameters.ContainsKey(offsets[2]) && parameters[offsets[2]].ToString() == "2";
        }

        public int Id { get; }

        public Vector2 Position { get; }

        public bool isCollected { get; }
    }
}
