using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VRise.Radar.Packets.Photon
{
    /// <summary>
    /// Helper class for Photon Protocol 18 robustness improvements
    /// Based on albiondata-client updates (2026-04-13/14)
    /// Provides endianness normalization, type conversion safety, and location extraction
    /// </summary>
    public static class PhotonProtocolHelper
    {
        // Known operation codes (partial list - extend as needed)
        private static readonly HashSet<ushort> KnownOperationCodes = new HashSet<ushort>
        {
            2,   // opJoin
            21,  // opMove
            218, // opAuctionGetOffers
            219, // opAuctionGetRequests
            // Add more known operation codes here
        };

        // Known event codes (partial list - extend as needed)
        private static readonly HashSet<ushort> KnownEventCodes = new HashSet<ushort>
        {
            29,  // NewCharacter
            46,  // Leave
            81,  // Move
            // Add more known event codes here
        };

        #region Type Conversion Safety

        /// <summary>
        /// Safely converts various integer types to ushort
        /// Handles int16, int8, uint8, int32, uint32, int64, uint64, and string
        /// Returns true if conversion succeeded
        /// </summary>
        public static bool TryConvertToUInt16(object value, out ushort result)
        {
            result = 0;

            if (value == null)
                return false;

            try
            {
                // Use type checks instead of pattern matching for .NET Framework 4.8 compatibility
                if (value is ushort)
                {
                    result = (ushort)value;
                    return true;
                }

                if (value is short)
                {
                    result = (ushort)(short)value;
                    return true;
                }

                if (value is byte)
                {
                    result = (byte)value;
                    return true;
                }

                if (value is sbyte)
                {
                    result = (ushort)(short)(sbyte)value;
                    return true;
                }

                if (value is int)
                {
                    result = (ushort)(int)value;
                    return true;
                }

                if (value is uint)
                {
                    result = (ushort)(uint)value;
                    return true;
                }

                if (value is long)
                {
                    result = (ushort)(long)value;
                    return true;
                }

                if (value is ulong)
                {
                    result = (ushort)(ulong)value;
                    return true;
                }

                if (value is string)
                {
                    if (ushort.TryParse((string)value, out var parsed))
                    {
                        result = parsed;
                        return true;
                    }
                    return false;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets operation code from parameters with type safety
        /// Checks params[253] and normalizes endianness
        /// </summary>
        public static bool TryGetOperationCode(Dictionary<byte, object> parameters, out ushort opCode)
        {
            opCode = 0;

            if (!parameters.ContainsKey(253))
                return false;

            if (!TryConvertToUInt16(parameters[253], out var rawCode))
                return false;

            opCode = NormalizeOperationCode(rawCode);
            return true;
        }

        /// <summary>
        /// Gets event code from parameters with type safety
        /// Checks params[252] and normalizes endianness
        /// </summary>
        public static bool TryGetEventCode(Dictionary<byte, object> parameters, out ushort eventCode)
        {
            eventCode = 0;

            if (!parameters.ContainsKey(252))
                return false;

            if (!TryConvertToUInt16(parameters[252], out var rawCode))
                return false;

            eventCode = NormalizeEventCode(rawCode);
            return true;
        }

        #endregion

        #region Endianness Normalization

        /// <summary>
        /// Normalizes operation code by detecting and correcting endianness issues
        /// Handles cases like 0xDA00 (should be 0x00DA)
        /// </summary>
        public static ushort NormalizeOperationCode(ushort code)
        {
            // Already known - return as-is
            if (IsKnownOperationCode(code))
                return code;

            // Try byte-swapped version
            ushort swapped = ByteSwap(code);
            if (IsKnownOperationCode(swapped))
                return swapped;

            // Handle common post-update artifact: 0xDA00 → 0x00DA
            if (code > 0x00FF && (code & 0x00FF) == 0)
            {
                ushort shifted = (ushort)(code >> 8);
                if (IsKnownOperationCode(shifted))
                    return shifted;
            }

            // Return original if no normalization worked
            return code;
        }

        /// <summary>
        /// Normalizes event code by detecting and correcting endianness issues
        /// </summary>
        public static ushort NormalizeEventCode(ushort code)
        {
            if (IsKnownEventCode(code))
                return code;

            ushort swapped = ByteSwap(code);
            if (IsKnownEventCode(swapped))
                return swapped;

            if (code > 0x00FF && (code & 0x00FF) == 0)
            {
                ushort shifted = (ushort)(code >> 8);
                if (IsKnownEventCode(shifted))
                    return shifted;
            }

            return code;
        }

        private static ushort ByteSwap(ushort value)
        {
            return (ushort)((value << 8) | (value >> 8));
        }

        private static bool IsKnownOperationCode(ushort code)
        {
            return KnownOperationCodes.Contains(code);
        }

        private static bool IsKnownEventCode(ushort code)
        {
            return KnownEventCodes.Contains(code);
        }

        #endregion

        #region Location Extraction (opJoin Fallback)

        /// <summary>
        /// Extracts location string from parameters with fallback logic
        /// Checks params[8] first, then searches recursively
        /// Returns null if no valid location found
        /// </summary>
        public static string ExtractLocation(Dictionary<byte, object> parameters, byte preferredKey = 8)
        {
            // Try preferred key first
            if (parameters.ContainsKey(preferredKey) && parameters[preferredKey] is string location)
            {
                var normalized = NormalizeLocationId(location);
                if (!string.IsNullOrEmpty(normalized))
                    return normalized;
            }

            // Fallback: search all parameters recursively
            foreach (var kvp in parameters.Values)
            {
                var found = ExtractLocationFromValue(kvp);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Checks if parameters look like a Join response
        /// Used as heuristic when opCode detection fails
        /// </summary>
        public static bool LooksLikeJoinResponse(Dictionary<byte, object> parameters)
        {
            // Check if params[8] contains location-like string
            if (parameters.ContainsKey(8) && parameters[8] is string loc)
            {
                if (!string.IsNullOrEmpty(NormalizeLocationId(loc)))
                    return true;
            }

            // Check if any parameter contains island/location keywords
            foreach (var value in parameters.Values)
            {
                if (value is string str)
                {
                    var lower = str.ToLower();
                    if (str.Contains("@player-island") || str.Contains("@island-") ||
                        lower.Contains("island") || str.Contains("@ISLAND@"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string ExtractLocationFromValue(object value)
        {
            // Use type checks instead of pattern matching for .NET Framework 4.8 compatibility
            if (value is string)
            {
                return ExtractLocationFromText((string)value);
            }

            if (value is Dictionary<byte, object>)
            {
                var dict = (Dictionary<byte, object>)value;
                foreach (var v in dict.Values)
                {
                    var found = ExtractLocationFromValue(v);
                    if (found != null)
                        return found;
                }
                return null;
            }

            if (value is Dictionary<object, object>)
            {
                var dict2 = (Dictionary<object, object>)value;
                foreach (var v in dict2.Values)
                {
                    var found = ExtractLocationFromValue(v);
                    if (found != null)
                        return found;
                }
                return null;
            }

            if (value is Array)
            {
                var arr = (Array)value;
                foreach (var item in arr)
                {
                    var found = ExtractLocationFromValue(item);
                    if (found != null)
                        return found;
                }
                return null;
            }

            if (value is byte[])
            {
                try
                {
                    var bytes = (byte[])value;
                    var text = System.Text.Encoding.UTF8.GetString(bytes);
                    return ExtractLocationFromText(text);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static string ExtractLocationFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            // Try direct normalization first
            var normalized = NormalizeLocationId(text);
            if (!string.IsNullOrEmpty(normalized))
                return normalized;

            // Split on non-printable characters and scan printable chunks
            var chunks = Regex.Split(text, @"[^\x20-\x7E]+")
                .Where(s => !string.IsNullOrWhiteSpace(s));

            foreach (var chunk in chunks)
            {
                normalized = NormalizeLocationId(chunk);
                if (!string.IsNullOrEmpty(normalized))
                    return normalized;

                var lower = chunk.ToLower();
                if (chunk.Contains("@player-island") || chunk.Contains("@island-") || lower.Contains("island"))
                    return NormalizeLocationId(chunk);
            }

            return null;
        }

        private static string NormalizeLocationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim().Trim(',', '.');

            // Island patterns
            if (Regex.IsMatch(trimmed, @"(?i)@island@[0-9a-f-]{36}"))
            {
                var match = Regex.Match(trimmed, @"(?i)@island@([0-9a-f-]{36})");
                if (match.Success)
                    return "@ISLAND@" + match.Groups[1].Value;
            }

            // Numeric cluster IDs (3-6 digits)
            if (Regex.IsMatch(trimmed, @"^[0-9]{3,6}$"))
                return trimmed;

            // Known location prefixes
            var lower = trimmed.ToLower();
            if (lower.StartsWith("island-player-") ||
                lower.StartsWith("@player-island") ||
                lower.StartsWith("@island-") ||
                trimmed.StartsWith("BLACKBANK-") ||
                trimmed.EndsWith("-HellDen") ||
                trimmed.EndsWith("-Auction2"))
            {
                return trimmed;
            }

            return null;
        }

        #endregion

        #region Public Utility: Add Known Codes at Runtime

        /// <summary>
        /// Registers additional known operation codes
        /// Call this during initialization to extend the known code list
        /// </summary>
        public static void RegisterOperationCodes(params ushort[] codes)
        {
            foreach (var code in codes)
            {
                KnownOperationCodes.Add(code);
            }
        }

        /// <summary>
        /// Registers additional known event codes
        /// Call this during initialization to extend the known code list
        /// </summary>
        public static void RegisterEventCodes(params ushort[] codes)
        {
            foreach (var code in codes)
            {
                KnownEventCodes.Add(code);
            }
        }

        #endregion
    }
}
