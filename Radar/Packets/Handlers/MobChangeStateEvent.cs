using Albion.Network;
using System;
using System.Collections.Generic;

namespace VRise.Radar.Packets.Handlers
{
    public class MobChangeStateEvent : BaseEvent
    {
        byte[] offsets = Init.PacketOffsets.MobChangeState;

        public MobChangeStateEvent(Dictionary<byte, object> parameters): base(parameters)
        {
            Id = Convert.ToInt32(parameters[offsets[0]]);

            Charge = Convert.ToInt32(parameters[offsets[1]]);
        }

        public int Id { get; }

        public int Charge { get; }
    }
}
