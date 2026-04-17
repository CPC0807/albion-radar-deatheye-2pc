using Albion.Network;
using PhotonPackageParser;
using System;
using System.Collections.Generic;
using System.IO;

namespace VRise.Radar.Packets.Photon
{
    /// <summary>
    /// Photon Protocol 18 packet wrapper
    /// Parses raw Photon UDP/TCP payloads using Protocol 18 deserialization
    /// Then creates compatible packets for the existing Albion.Network IPhotonReceiver
    /// Reference: https://github.com/ao-data/albiondata-client/blob/master/client/photon/parser.go
    /// </summary>
    public class PhotonParser
    {
        private const int PhotonHeaderLength = 12;
        private const int CommandHeaderLength = 12;

        // Photon command type constants
        private const byte CmdDisconnect = 4;
        private const byte CmdSendReliable = 6;
        private const byte CmdSendUnreliable = 7;
        private const byte CmdSendFragment = 8;

        // Photon reliable message type constants
        private const byte MsgRequest = 2;
        private const byte MsgResponse = 3;
        private const byte MsgEvent = 4;
        private const byte MsgResponseAlt = 7;  // Some Albion builds use type 7 for response
        private const byte MsgEncrypted = 131;

        private readonly Dictionary<int, SegmentedPackage> pendingSegments = new Dictionary<int, SegmentedPackage>();
        private readonly IPhotonReceiver photonReceiver;

        public Action OnEncrypted { get; set; }

        public PhotonParser(IPhotonReceiver photonReceiver)
        {
            this.photonReceiver = photonReceiver ?? throw new ArgumentNullException(nameof(photonReceiver));
        }

        /// <summary>
        /// Processes a raw Photon UDP/TCP payload
        /// Parses with Protocol 18, then forwards to IPhotonReceiver
        /// Returns true if the packet header was valid
        /// </summary>
        public bool ReceivePacket(byte[] payload)
        {
            if (payload == null || payload.Length < PhotonHeaderLength)
                return false;

            try
            {
                int offset = 2; // skip peerId (2 bytes)
                byte flags = payload[offset++];
                int commandCount = payload[offset++];
                offset += 8; // skip timestamp (4) + challenge (4)

                Console.WriteLine($"[PhotonParser] Received packet: flags={flags}, commands={commandCount}, length={payload.Length}");

                // Encrypted packet
                if (flags == 1)
                {
                    Console.WriteLine($"[PhotonParser] Encrypted packet - skipping");
                    OnEncrypted?.Invoke();
                    return false;
                }

                for (int i = 0; i < commandCount; i++)
                {
                    offset = HandleCommand(payload, offset);
                    if (offset < 0)
                    {
                        Console.WriteLine($"[PhotonParser] HandleCommand failed at command {i}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PhotonParser] Error parsing packet: {ex.Message}");
                Console.WriteLine($"[PhotonParser] StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        private int HandleCommand(byte[] src, int offset)
        {
            if (offset + CommandHeaderLength > src.Length)
                return -1;

            byte cmdType = src[offset++];
            offset++; // channelId
            offset++; // commandFlags
            offset++; // reserved byte

            int cmdLen = (src[offset] << 24) | (src[offset + 1] << 16) | (src[offset + 2] << 8) | src[offset + 3];
            offset += 4;
            offset += 4; // reliableSequenceNumber

            cmdLen -= CommandHeaderLength;

            if (cmdLen < 0 || offset + cmdLen > src.Length)
                return -1;

            switch (cmdType)
            {
                case CmdDisconnect:
                    return offset + cmdLen;

                case CmdSendUnreliable:
                    if (cmdLen < 4)
                        return offset + cmdLen;
                    offset += 4;
                    cmdLen -= 4;
                    HandleSendReliable(src, offset, cmdLen);
                    return offset + cmdLen;

                case CmdSendReliable:
                    HandleSendReliable(src, offset, cmdLen);
                    return offset + cmdLen;

                case CmdSendFragment:
                    return HandleSendFragment(src, offset, cmdLen);

                default:
                    return offset + cmdLen;
            }
        }

        private void HandleSendReliable(byte[] src, int offset, int cmdLen)
        {
            if (cmdLen < 2 || offset + cmdLen > src.Length)
                return;

            // byte signalByte = src[offset];
            offset++;
            byte msgType = src[offset++];
            cmdLen -= 2;

            Console.WriteLine($"[PhotonParser] HandleSendReliable: msgType={msgType}, cmdLen={cmdLen}");

            if (offset + cmdLen > src.Length)
                return;

            // Check for encrypted message
            if (msgType == MsgEncrypted)
            {
                Console.WriteLine($"[PhotonParser] Encrypted message - skipping");
                OnEncrypted?.Invoke();
                return;
            }

            var data = new byte[cmdLen];
            Array.Copy(src, offset, data, 0, cmdLen);

            // Parse with Protocol 18 and reconstruct packet for IPhotonReceiver
            try
            {
                var reconstructedPacket = ReconstructPacketForReceiver(msgType, data);
                if (reconstructedPacket != null)
                {
                    Console.WriteLine($"[PhotonParser] Forwarding reconstructed packet to receiver (length={reconstructedPacket.Length})");
                    // Pass the reconstructed packet to the old receiver
                    photonReceiver.ReceivePacket(reconstructedPacket);
                }
                else
                {
                    Console.WriteLine($"[PhotonParser] ReconstructPacketForReceiver returned null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PhotonParser] Error reconstructing packet: {ex.Message}");
                Console.WriteLine($"[PhotonParser] StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Reconstructs a packet that's compatible with the old Protocol 16 receiver
        /// by parsing Protocol 18 data and converting parameter table
        /// </summary>
        private byte[] ReconstructPacketForReceiver(byte msgType, byte[] data)
        {
            try
            {
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    // Write message type
                    writer.Write(msgType);

                    if (msgType == MsgRequest)
                    {
                        if (data.Length < 1)
                        {
                            Console.WriteLine($"[PhotonParser] Request data too short: {data.Length}");
                            return null;
                        }

                        byte opCode = data[0];
                        Console.WriteLine($"[PhotonParser] Parsing Request opCode={opCode}");

                        var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, 1));
                        Console.WriteLine($"[PhotonParser] Request parsed: {parameters.Count} parameters");

                        // Ensure params[253] contains the operation code
                        if (!parameters.ContainsKey(253))
                        {
                            parameters[253] = (int)opCode;
                        }

                        // Write opCode
                        writer.Write(opCode);

                        // Write parameter count and parameters
                        WriteParameterTable(writer, parameters);
                }
                else if (msgType == MsgResponse || msgType == MsgResponseAlt)
                {
                    if (data.Length < 3)
                        return null;

                    byte opCode = data[0];
                    short returnCode = (short)(data[1] | (data[2] << 8));

                    int offset = 3;
                    string debugMessage = "";

                    // Read debug message if present
                    if (offset < data.Length)
                    {
                        byte debugType = data[offset++];
                        using (var stream = new MemoryStream(data, offset, data.Length - offset))
                        {
                            try
                            {
                                var debugObj = DeserializeSingle(stream, debugType);
                                if (debugObj is string str)
                                {
                                    debugMessage = str;
                                }
                                offset += (int)stream.Position;
                            }
                            catch
                            {
                                // If debug message parsing fails, just skip it
                            }
                        }
                    }

                    var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, offset));

                    // Ensure params[253] contains the operation code
                    if (!parameters.ContainsKey(253))
                    {
                        parameters[253] = (int)opCode;
                    }

                    // Write opCode, returnCode, debugMessage
                    writer.Write(opCode);
                    writer.Write(returnCode);
                    writer.Write((byte)(string.IsNullOrEmpty(debugMessage) ? 0 : 1));
                    if (!string.IsNullOrEmpty(debugMessage))
                    {
                        WriteString(writer, debugMessage);
                    }

                    // Write parameters
                    WriteParameterTable(writer, parameters);
                }
                else if (msgType == MsgEvent)
                {
                    if (data.Length < 1)
                        return null;

                    byte code = data[0];
                    var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, 1));

                    // Ensure params[252] contains the event code
                    if (!parameters.ContainsKey(252))
                    {
                        parameters[252] = (int)code;
                    }

                    // Write event code
                    writer.Write(code);

                    // Write parameters
                    WriteParameterTable(writer, parameters);
                }

                return ms.ToArray();
            }
        }

        private void WriteParameterTable(BinaryWriter writer, Dictionary<byte, object> parameters)
        {
            writer.Write((byte)parameters.Count);

            foreach (var kvp in parameters)
            {
                writer.Write(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
        }

        private void WriteValue(BinaryWriter writer, object value)
        {
            if (value == null)
            {
                writer.Write((byte)42); // Null type
                return;
            }

            Type t = value.GetType();

            if (t == typeof(bool))
            {
                writer.Write((byte)2); // Boolean
                writer.Write((bool)value);
            }
            else if (t == typeof(byte))
            {
                writer.Write((byte)3); // Byte
                writer.Write((byte)value);
            }
            else if (t == typeof(short))
            {
                writer.Write((byte)4); // Short
                writer.Write((short)value);
            }
            else if (t == typeof(int))
            {
                writer.Write((byte)105); // Int32
                writer.Write((int)value);
            }
            else if (t == typeof(long))
            {
                writer.Write((byte)108); // Long
                writer.Write((long)value);
            }
            else if (t == typeof(float))
            {
                writer.Write((byte)102); // Float
                writer.Write((float)value);
            }
            else if (t == typeof(double))
            {
                writer.Write((byte)100); // Double
                writer.Write((double)value);
            }
            else if (t == typeof(string))
            {
                writer.Write((byte)115); // String
                WriteString(writer, (string)value);
            }
            else if (t == typeof(byte[]))
            {
                byte[] arr = (byte[])value;
                writer.Write((byte)120); // ByteArray
                writer.Write(arr.Length);
                writer.Write(arr);
            }
            else if (t.IsArray)
            {
                Array arr = (Array)value;
                writer.Write((byte)121); // Array
                writer.Write((short)arr.Length);
                foreach (var item in arr)
                {
                    WriteValue(writer, item);
                }
            }
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                writer.Write((byte)68); // Dictionary
                var dict = value as System.Collections.IDictionary;
                writer.Write((short)dict.Count);
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    WriteValue(writer, entry.Key);
                    WriteValue(writer, entry.Value);
                }
            }
            else
            {
                // Unknown type - write as null
                writer.Write((byte)42);
            }
        }

        private void WriteString(BinaryWriter writer, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                writer.Write((short)0);
            }
            else
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(str);
                writer.Write((short)bytes.Length);
                writer.Write(bytes);
            }
        }

        private object DeserializeSingle(MemoryStream stream, byte typeCode)
        {
            // Create a small wrapper to deserialize a single value
            var buffer = new byte[stream.Length - stream.Position + 3];
            buffer[0] = 1; // count = 1
            buffer[1] = 0; // key = 0
            buffer[2] = typeCode;
            stream.Read(buffer, 3, buffer.Length - 3);

            var result = Protocol18Deserializer.DeserializeParameterTable(buffer);
            return result.ContainsKey(0) ? result[0] : null;
        }

        private int HandleSendFragment(byte[] src, int offset, int cmdLen)
        {
            if (offset + 20 > src.Length) // Fragment header is 20 bytes
                return offset + cmdLen;

            // Fragment header: startSeq(4) fragCount(4) fragNum(4) totalLen(4) fragOffset(4)
            int startSeq = (src[offset] << 24) | (src[offset + 1] << 16) | (src[offset + 2] << 8) | src[offset + 3];
            // int fragCount = (src[offset + 4] << 24) | (src[offset + 5] << 16) | (src[offset + 6] << 8) | src[offset + 7];
            // int fragNum = (src[offset + 8] << 24) | (src[offset + 9] << 16) | (src[offset + 10] << 8) | src[offset + 11];
            int totalLen = (src[offset + 12] << 24) | (src[offset + 13] << 16) | (src[offset + 14] << 8) | src[offset + 15];
            int fragOffset = (src[offset + 16] << 24) | (src[offset + 17] << 16) | (src[offset + 18] << 8) | src[offset + 19];

            offset += 20;
            cmdLen -= 20;

            if (!pendingSegments.TryGetValue(startSeq, out var package))
            {
                package = new SegmentedPackage
                {
                    TotalLength = totalLen,
                    Payload = new byte[totalLen],
                    BytesWritten = 0
                };
                pendingSegments[startSeq] = package;
            }

            // Copy fragment data
            int copyLen = Math.Min(cmdLen, package.Payload.Length - fragOffset);
            if (copyLen > 0 && offset + copyLen <= src.Length)
            {
                Array.Copy(src, offset, package.Payload, fragOffset, copyLen);
                package.BytesWritten += copyLen;

                // Check if complete
                if (package.BytesWritten >= package.TotalLength)
                {
                    HandleSendReliable(package.Payload, 0, package.TotalLength);
                    pendingSegments.Remove(startSeq);
                }
            }

            return offset + cmdLen;
        }

        private byte[] SubArray(byte[] data, int start)
        {
            if (start >= data.Length)
                return new byte[0];

            var result = new byte[data.Length - start];
            Array.Copy(data, start, result, 0, result.Length);
            return result;
        }

        private class SegmentedPackage
        {
            public int TotalLength { get; set; }
            public byte[] Payload { get; set; }
            public int BytesWritten { get; set; }
        }
    }
}
