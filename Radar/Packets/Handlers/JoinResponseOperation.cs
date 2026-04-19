using Albion.Network;
using VRise.Radar.Utility;
using VRise.Radar.Packets.Photon;
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

            // Location extraction with fallback logic (handles game updates)
            Location = ExtractLocationSafely(parameters, offsets[4]);

            Faction = (Faction)Convert.ToByte(parameters[offsets[5]]);

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

        /// <summary>
        /// Safely extracts location from parameters with fallback logic
        /// Based on albiondata-client's approach to handle protocol changes
        /// </summary>
        private string ExtractLocationSafely(Dictionary<byte, object> parameters, byte preferredKey)
        {
            // Try direct access first
            if (parameters.ContainsKey(preferredKey) && parameters[preferredKey] is string directLocation)
            {
                if (!string.IsNullOrWhiteSpace(directLocation))
                    return directLocation;
            }

            // Fallback: use helper to search recursively
            var fallbackLocation = PhotonProtocolHelper.ExtractLocation(parameters, preferredKey);
            if (fallbackLocation != null)
            {
                #if DEBUG
                Console.WriteLine($"[JoinResponse] Location extracted via fallback: {fallbackLocation}");
                #endif
                return fallbackLocation;
            }

            // Last resort: return placeholder
            #if DEBUG
            Console.WriteLine("[JoinResponse] WARNING: Could not extract location from parameters");
            #endif
            return "UNKNOWN";
        }
    }
}
