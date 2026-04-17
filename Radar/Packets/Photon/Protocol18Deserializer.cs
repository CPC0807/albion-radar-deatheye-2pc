using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VRise.Radar.Packets.Photon
{
    /// <summary>
    /// Protocol 18 deserializer - port of albiondata-client's Protocol18 implementation
    /// Handles Photon Protocol 18 type codes and deserialization
    /// Reference: https://github.com/ao-data/albiondata-client/blob/master/client/photon/deserializer.go
    /// </summary>
    public static class Protocol18Deserializer
    {
        // Protocol 18 type codes
        private const byte TypeUnknown = 0;
        private const byte TypeBoolean = 2;
        private const byte TypeByte = 3;
        private const byte TypeShort = 4;
        private const byte TypeFloat = 5;
        private const byte TypeDouble = 6;
        private const byte TypeString = 7;
        private const byte TypeNull = 8;
        private const byte TypeCompressedInt = 9;
        private const byte TypeCompressedLong = 10;
        private const byte TypeInt1 = 11;       // 1-byte unsigned int
        private const byte TypeInt1Neg = 12;    // 1-byte unsigned int, negated
        private const byte TypeInt2 = 13;       // 2-byte unsigned int
        private const byte TypeInt2Neg = 14;    // 2-byte unsigned int, negated
        private const byte TypeLong1 = 15;      // 1-byte unsigned long
        private const byte TypeLong1Neg = 16;   // 1-byte unsigned long, negated
        private const byte TypeLong2 = 17;      // 2-byte unsigned long
        private const byte TypeLong2Neg = 18;   // 2-byte unsigned long, negated
        private const byte TypeCustom = 19;
        private const byte TypeDictionary = 20;
        private const byte TypeHashtable = 21;
        private const byte TypeObjectArray = 23;
        private const byte TypeOperationRequest = 24;
        private const byte TypeOperationResp = 25;
        private const byte TypeEventData = 26;
        private const byte TypeBoolFalse = 27;
        private const byte TypeBoolTrue = 28;
        private const byte TypeShortZero = 29;
        private const byte TypeIntZero = 30;
        private const byte TypeLongZero = 31;
        private const byte TypeFloatZero = 32;
        private const byte TypeDoubleZero = 33;
        private const byte TypeByteZero = 34;
        private const byte TypeArray = 0x40;
        private const byte CustomTypeSlimBase = 0x80;

        /// <summary>
        /// Deserializes a Protocol 18 parameter table
        /// Wire format: compressed-varint count | (key | typeCode | value)*
        /// </summary>
        public static Dictionary<byte, object> DeserializeParameterTable(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return ReadParameterTable(stream);
            }
        }

        private static Dictionary<byte, object> ReadParameterTable(Stream stream)
        {
            int count = (int)ReadCount(stream);
            var parameters = new Dictionary<byte, object>(count);

            for (int i = 0; i < count && stream.Position < stream.Length; i++)
            {
                byte key = (byte)stream.ReadByte();
                byte typeCode = (byte)stream.ReadByte();
                object value = Deserialize(stream, typeCode);
                parameters[key] = value;
            }

            return parameters;
        }

        private static object Deserialize(Stream stream, byte typeCode)
        {
            // Handle slim custom types (>=0x80)
            if (typeCode >= CustomTypeSlimBase)
            {
                return DeserializeCustom(stream, typeCode);
            }

            switch (typeCode)
            {
                case TypeUnknown:
                case TypeNull:
                    return null;

                case TypeBoolean:
                    return stream.ReadByte() != 0;

                case TypeByte:
                    return (byte)stream.ReadByte();

                case TypeShort:
                    return ReadInt16(stream);

                case TypeFloat:
                    return ReadFloat32(stream);

                case TypeDouble:
                    return ReadFloat64(stream);

                case TypeString:
                    return ReadString(stream);

                case TypeCompressedInt:
                    return ReadCompressedInt32(stream);

                case TypeCompressedLong:
                    return ReadCompressedInt64(stream);

                case TypeInt1:
                    return (int)stream.ReadByte();

                case TypeInt1Neg:
                    return -(int)stream.ReadByte();

                case TypeInt2:
                    return (int)ReadUInt16(stream);

                case TypeInt2Neg:
                    return -(int)ReadUInt16(stream);

                case TypeLong1:
                    return (long)stream.ReadByte();

                case TypeLong1Neg:
                    return -(long)stream.ReadByte();

                case TypeLong2:
                    return (long)ReadUInt16(stream);

                case TypeLong2Neg:
                    return -(long)ReadUInt16(stream);

                case TypeCustom:
                    return DeserializeCustom(stream, 0);

                case TypeDictionary:
                    return DeserializeDictionary(stream);

                case TypeHashtable:
                    return DeserializeDictionary(stream);

                case TypeObjectArray:
                    return DeserializeObjectArray(stream);

                case TypeBoolFalse:
                    return false;

                case TypeBoolTrue:
                    return true;

                case TypeShortZero:
                    return (short)0;

                case TypeIntZero:
                    return 0;

                case TypeLongZero:
                    return 0L;

                case TypeFloatZero:
                    return 0f;

                case TypeDoubleZero:
                    return 0.0;

                case TypeByteZero:
                    return (byte)0;

                case TypeArray:
                    return DeserializeNestedArray(stream);

                default:
                    // Check for typed array (0x40 | elementType)
                    if ((typeCode & TypeArray) == TypeArray)
                    {
                        byte elementType = (byte)(typeCode & ~TypeArray);
                        return DeserializeTypedArray(stream, elementType);
                    }
                    return $"ERROR - unknown type 0x{typeCode:X2}";
            }
        }

        private static object DeserializeTypedArray(Stream stream, byte elementType)
        {
            int size = (int)ReadCount(stream);

            switch (elementType)
            {
                case TypeBoolean:
                    var bools = new bool[size];
                    int packedBytes = (size + 7) / 8;
                    var packed = new byte[packedBytes];
                    stream.Read(packed, 0, packedBytes);
                    for (int i = 0; i < size; i++)
                    {
                        bools[i] = (packed[i / 8] & (1 << (i % 8))) != 0;
                    }
                    return bools;

                case TypeByte:
                    var bytes = new byte[size];
                    stream.Read(bytes, 0, size);
                    return bytes;

                case TypeShort:
                    var shorts = new short[size];
                    for (int i = 0; i < size; i++)
                        shorts[i] = ReadInt16(stream);
                    return shorts;

                case TypeFloat:
                    var floats = new float[size];
                    for (int i = 0; i < size; i++)
                        floats[i] = ReadFloat32(stream);
                    return floats;

                case TypeDouble:
                    var doubles = new double[size];
                    for (int i = 0; i < size; i++)
                        doubles[i] = ReadFloat64(stream);
                    return doubles;

                case TypeString:
                    var strings = new string[size];
                    for (int i = 0; i < size; i++)
                        strings[i] = ReadString(stream);
                    return strings;

                case TypeCompressedInt:
                    var ints = new int[size];
                    for (int i = 0; i < size; i++)
                        ints[i] = ReadCompressedInt32(stream);
                    return ints;

                case TypeCompressedLong:
                    var longs = new long[size];
                    for (int i = 0; i < size; i++)
                        longs[i] = ReadCompressedInt64(stream);
                    return longs;

                default:
                    var objects = new object[size];
                    for (int i = 0; i < size; i++)
                        objects[i] = Deserialize(stream, elementType);
                    return objects;
            }
        }

        private static object DeserializeNestedArray(Stream stream)
        {
            int size = (int)ReadCount(stream);
            byte typeCode = (byte)stream.ReadByte();
            var array = new object[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = Deserialize(stream, typeCode);
            }
            return array;
        }

        private static object DeserializeObjectArray(Stream stream)
        {
            int size = (int)ReadCount(stream);
            var array = new object[size];
            for (int i = 0; i < size; i++)
            {
                byte typeCode = (byte)stream.ReadByte();
                array[i] = Deserialize(stream, typeCode);
            }
            return array;
        }

        private static Dictionary<object, object> DeserializeDictionary(Stream stream)
        {
            byte keyType = (byte)stream.ReadByte();
            byte valueType = (byte)stream.ReadByte();
            int count = (int)ReadCount(stream);

            var dict = new Dictionary<object, object>(count);

            for (int i = 0; i < count && stream.Position < stream.Length; i++)
            {
                byte kt = keyType == 0 ? (byte)stream.ReadByte() : keyType;
                byte vt = valueType == 0 ? (byte)stream.ReadByte() : valueType;

                object key = Deserialize(stream, kt);
                object value = Deserialize(stream, vt);

                // Handle unhashable keys
                if (key == null || key.GetType().IsArray)
                {
                    dict[$"UNHASHABLE_{i}_{key?.GetType().Name}"] = value;
                }
                else
                {
                    dict[key] = value;
                }
            }

            return dict;
        }

        private static object DeserializeCustom(Stream stream, byte gpType)
        {
            byte customId;
            bool isSlim = gpType >= CustomTypeSlimBase;

            if (isSlim)
            {
                customId = (byte)(gpType & 0x7F);
            }
            else
            {
                customId = (byte)stream.ReadByte();
            }

            int size = (int)ReadCount(stream);
            var data = new byte[size];
            stream.Read(data, 0, size);

            return new Dictionary<string, object>
            {
                { "type", customId },
                { "data", data }
            };
        }

        // ── Low-level readers ────────────────────────────────────────────────

        private static short ReadInt16(Stream stream)
        {
            byte b1 = (byte)stream.ReadByte();
            byte b2 = (byte)stream.ReadByte();
            return (short)(b1 | (b2 << 8)); // Little-endian
        }

        private static ushort ReadUInt16(Stream stream)
        {
            byte b1 = (byte)stream.ReadByte();
            byte b2 = (byte)stream.ReadByte();
            return (ushort)(b1 | (b2 << 8)); // Little-endian
        }

        private static float ReadFloat32(Stream stream)
        {
            var bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        private static double ReadFloat64(Stream stream)
        {
            var bytes = new byte[8];
            stream.Read(bytes, 0, 8);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        private static string ReadString(Stream stream)
        {
            int length = (int)ReadCount(stream);
            if (length == 0)
                return string.Empty;

            var bytes = new byte[length];
            stream.Read(bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        private static uint ReadCount(Stream stream)
        {
            uint result = 0;
            int shift = 0;

            while (true)
            {
                byte b = (byte)stream.ReadByte();
                result |= (uint)(b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                    break;

                shift += 7;
            }

            return result;
        }

        private static int ReadCompressedInt32(Stream stream)
        {
            uint value = ReadCount(stream);
            // Zig-zag decode: (n >> 1) ^ -(n & 1)
            return (int)((value >> 1) ^ (-(int)(value & 1)));
        }

        private static long ReadCompressedInt64(Stream stream)
        {
            ulong result = 0;
            int shift = 0;

            while (true)
            {
                byte b = (byte)stream.ReadByte();
                result |= (ulong)(b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                    break;

                shift += 7;
            }

            // Zig-zag decode: (n >> 1) ^ (-(n & 1))
            // Convert to signed, then apply XOR
            return (long)((result >> 1) ^ (ulong)(-(long)(result & 1)));
        }
    }
}
