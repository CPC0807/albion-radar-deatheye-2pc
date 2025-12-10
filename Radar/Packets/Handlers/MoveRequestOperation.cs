using Albion.Network;
using VRise.Radar.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace VRise.Radar.Packets.Handlers
{
    public class MoveRequestOperation : BaseOperation
    {
        byte[] offsets = Init.PacketOffsets.MoveRequest;
        
        public MoveRequestOperation(Dictionary<byte, object> parameters) : base(parameters)
        {
            // 安全的類型轉換，避免 InvalidCastException
            try
            {
                Position = parameters[offsets[0]] is float[] pos ? Additions.fromFArray(pos) : Vector2.Zero;
                NewPosition = parameters[offsets[1]] is float[] newPos ? Additions.fromFArray(newPos) : Vector2.Zero;
                Speed = parameters.ContainsKey(offsets[2]) ? (float)parameters[offsets[2]] : 0f;
                Time = DateTime.UtcNow;
            }
            catch (InvalidCastException)
            {
                Position = Vector2.Zero;
                NewPosition = Vector2.Zero;
                Speed = 0f;
                Time = DateTime.UtcNow;
            }
        }

        public Vector2 Position { get; }
        public Vector2 NewPosition { get; }
        public float Speed { get; }
        public DateTime Time { get; }
    }
}
