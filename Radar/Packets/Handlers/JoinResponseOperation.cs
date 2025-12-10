using Albion.Network;
using VRise.Radar.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace VRise.Radar.Packets.Handlers
{
    [Obfuscation(Feature = "mutation", Exclude = false)]
    public class JoinResponseOperation : BaseOperation
    {
        byte[] offsets = Init.PacketOffsets.JoinResponse;

        public JoinResponseOperation(Dictionary<byte, object> parameters) : base(parameters)
        {
            Id = Convert.ToInt32(parameters[offsets[0]]);
            Nick = parameters[offsets[1]] as string;

            Guild = parameters.ContainsKey(offsets[2]) ? parameters[offsets[2]] as string : "!";
            Alliance = parameters.ContainsKey(offsets[3]) ? parameters[offsets[3]] as string : "!";

            Location = parameters[offsets[4]] as string;

            Faction = (Faction)parameters[offsets[5]];

            // 安全的類型轉換，避免 InvalidCastException
            try
            {
                if (parameters[offsets[6]] is float[] floatArray)
                {
                    Position = Additions.fromFArray(floatArray);
                }
                else
                {
                    Position = Vector2.Zero;
                    #if DEBUG
                    Console.WriteLine($"[JoinResponse] WARNING: Position parameter is {parameters[offsets[6]]?.GetType().Name ?? "null"}, expected float[]");
                    #endif
                }
            }
            catch (Exception ex)
            {
                Position = Vector2.Zero;
                #if DEBUG
                Console.WriteLine($"[JoinResponse] ERROR: {ex.Message}");
                #endif
            }
        }

        public int Id { get; }
        public string Nick { get; }
        public Faction Faction { get; }
        public string Guild { get; }
        public string Alliance { get; }
        public string Location { get; }
        public Vector2 Position { get; }
    }
}
