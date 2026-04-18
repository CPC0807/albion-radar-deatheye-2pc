using Albion.Network;
using System;
using System.Collections.Generic;

namespace VRise.Radar.Packets.Handlers
{
    public class LeaveEvent : BaseEvent
    {
        byte[] offsets = Init.PacketOffsets.Leave;

        public LeaveEvent(Dictionary<byte, object> parameters) : base(parameters)
        {
            Id = 0;
            if (parameters.TryGetValue(offsets[0], out var v) && v != null)
            {
                if (v is IConvertible)
                {
                    try { Id = Convert.ToInt32(v); } catch { Id = 0; }
                }
                else if (v is byte[] b && b.Length >= 4)
                {
                    Id = BitConverter.ToInt32(b, 0);
                }
                // byte[][] / object[] / other shapes: not a classic Leave payload — skip
            }
        }

        public int Id { get; }
    }
}
