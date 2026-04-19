using Albion.Network;
using VRise.Radar.Utility;
using System;
using System.Collections.Generic;

namespace VRise.Radar.Packets.Handlers
{
    public class ChangeFlaggingFinishedEvent : BaseEvent
    {
        byte[] offsets = Init.PacketOffsets.ChangeFlaggingFinished;

        public ChangeFlaggingFinishedEvent(Dictionary<byte, object> parameters): base(parameters)
        {
            Id = Convert.ToInt32(parameters[offsets[0]]);
            Faction = (Faction)Convert.ToByte(parameters[offsets[1]]);
        }
        
        public int Id { get; }
        public Faction Faction { get; }
    }
}
