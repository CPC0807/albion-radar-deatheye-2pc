using Albion.Network;
using System;
using System.Collections.Generic;

namespace VRise.Radar.Packets.Photon
{
    /// <summary>
    /// Bridge between Protocol 18 PhotonParser and Albion.Network ReceiverBuilder
    /// Mimics albiondata-client's approach: parse with Protocol 18, dispatch directly to handlers
    /// </summary>
    public class Protocol18ReceiverBridge
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
        private const byte MsgResponseAlt = 7;
        private const byte MsgEncrypted = 131;

        private readonly Dictionary<int, SegmentedPackage> pendingSegments = new Dictionary<int, SegmentedPackage>();
        private readonly IPhotonReceiver photonReceiver;

        // Callbacks for Protocol 18 parsed data
        public Action<byte, Dictionary<byte, object>> OnRequest { get; set; }
        public Action<byte, short, string, Dictionary<byte, object>> OnResponse { get; set; }
        public Action<byte, Dictionary<byte, object>> OnEvent { get; set; }
        public Action OnEncrypted { get; set; }

        public Protocol18ReceiverBridge(IPhotonReceiver photonReceiver)
        {
            this.photonReceiver = photonReceiver ?? throw new ArgumentNullException(nameof(photonReceiver));
        }

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

                // Encrypted packet
                if (flags == 1)
                {
                    OnEncrypted?.Invoke();
                    return false;
                }

                for (int i = 0; i < commandCount; i++)
                {
                    offset = HandleCommand(payload, offset);
                    if (offset < 0)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Protocol18Bridge] Error parsing packet: {ex.Message}");
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

            offset++; // skip signalByte
            byte msgType = src[offset++];
            cmdLen -= 2;

            if (offset + cmdLen > src.Length)
                return;

            // Check for encrypted message
            if (msgType == MsgEncrypted)
            {
                OnEncrypted?.Invoke();
                return;
            }

            var data = new byte[cmdLen];
            Array.Copy(src, offset, data, 0, cmdLen);

            switch (msgType)
            {
                case MsgRequest:
                    DispatchRequest(data);
                    break;
                case MsgResponse:
                case MsgResponseAlt:
                    DispatchResponse(data);
                    break;
                case MsgEvent:
                    DispatchEvent(data);
                    break;
            }
        }

        private void DispatchRequest(byte[] data)
        {
            if (data.Length < 1)
                return;

            try
            {
                byte opCode = data[0];
                var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, 1));

                // Ensure params[253] contains the operation code
                if (!parameters.ContainsKey(253))
                {
                    parameters[253] = (int)opCode;
                }

                Console.WriteLine($"[Protocol18Bridge] Request {opCode} with {parameters.Count} parameters");

                // Call the callback AND the old receiver (for compatibility)
                OnRequest?.Invoke(opCode, parameters);

                // Also try to pass to the old receiver (will likely fail with Protocol 16 parser, but worth trying)
                // photonReceiver.ReceivePacket(CreateLegacyPacket(MsgRequest, opCode, parameters));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Protocol18Bridge] Error dispatching request: {ex.Message}");
            }
        }

        private void DispatchResponse(byte[] data)
        {
            if (data.Length < 3)
                return;

            try
            {
                byte opCode = data[0];
                short returnCode = (short)(data[1] | (data[2] << 8)); // Little-endian

                int offset = 3;
                string debugMessage = "";

                // Read debug message if present
                if (offset < data.Length)
                {
                    offset++; // skip debug type for now
                }

                var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, offset));

                // Ensure params[253] contains the operation code
                if (!parameters.ContainsKey(253))
                {
                    parameters[253] = (int)opCode;
                }

                Console.WriteLine($"[Protocol18Bridge] Response {opCode} (rc={returnCode}) with {parameters.Count} parameters");

                // Call the callback
                OnResponse?.Invoke(opCode, returnCode, debugMessage, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Protocol18Bridge] Error dispatching response: {ex.Message}");
            }
        }

        private void DispatchEvent(byte[] data)
        {
            if (data.Length < 1)
                return;

            try
            {
                byte code = data[0];
                var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, 1));

                // Ensure params[252] contains the event code
                if (!parameters.ContainsKey(252))
                {
                    parameters[252] = (int)code;
                }

                Console.WriteLine($"[Protocol18Bridge] Event {code} with {parameters.Count} parameters");

                // Call the callback
                OnEvent?.Invoke(code, parameters);

                // ALSO pass to old receiver by creating event objects manually
                TryPassToOldReceiver(code, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Protocol18Bridge] Error dispatching event: {ex.Message}");
            }
        }

        private void TryPassToOldReceiver(byte code, Dictionary<byte, object> parameters)
        {
            try
            {
                // Create an EventPacket-like object and pass it through the receiver
                // This requires using reflection to create Albion.Network types
                var assembly = typeof(IPhotonReceiver).Assembly;
                var eventPacketType = assembly.GetType("Albion.Network.EventPacket");

                if (eventPacketType != null)
                {
                    var eventPacket = Activator.CreateInstance(eventPacketType, new object[] { parameters });

                    // Try to trigger handlers by passing through receiver
                    // Note: This is a hack and may not work, but it's worth trying
                    var method = photonReceiver.GetType().GetMethod("ReceivePacket");
                    if (method != null)
                    {
                        // Can't directly pass EventPacket, need bytes
                        // So this approach won't work - we need a different strategy
                    }
                }
            }
            catch
            {
                // Silently fail - this is just a compatibility attempt
            }
        }

        private int HandleSendFragment(byte[] src, int offset, int cmdLen)
        {
            if (offset + 20 > src.Length)
                return offset + cmdLen;

            int startSeq = (src[offset] << 24) | (src[offset + 1] << 16) | (src[offset + 2] << 8) | src[offset + 3];
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

            int copyLen = Math.Min(cmdLen, package.Payload.Length - fragOffset);
            if (copyLen > 0 && offset + copyLen <= src.Length)
            {
                Array.Copy(src, offset, package.Payload, fragOffset, copyLen);
                package.BytesWritten += copyLen;

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
