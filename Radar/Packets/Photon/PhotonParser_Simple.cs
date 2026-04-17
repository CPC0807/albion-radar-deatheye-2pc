using Albion.Network;
using System;
using System.Collections.Generic;

namespace VRise.Radar.Packets.Photon
{
    /// <summary>
    /// Simplified Photon Protocol 18 parser
    /// Bypasses packet reconstruction - directly uses Protocol 18 deserialized parameters
    /// </summary>
    public class PhotonParserSimple
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

        public Action OnEncrypted { get; set; }

        public PhotonParserSimple(IPhotonReceiver photonReceiver)
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
                Console.WriteLine($"[PhotonParserSimple] Error parsing packet: {ex.Message}");
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

            try
            {
                // Create a fake Protocol 16 packet with Protocol 18 data
                // The ReceiverBuilder will handle it with our handlers
                var fakePacket = CreateFakePacket(msgType, data);
                if (fakePacket != null)
                {
                    photonReceiver.ReceivePacket(fakePacket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PhotonParserSimple] Error dispatching: {ex.Message}");
            }
        }

        private byte[] CreateFakePacket(byte msgType, byte[] protocol18Data)
        {
            // Simply prepend the message type to the Protocol 18 data
            // The Albion.Network library will parse it
            var packet = new byte[protocol18Data.Length + 1];
            packet[0] = msgType;
            Array.Copy(protocol18Data, 0, packet, 1, protocol18Data.Length);
            return packet;
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

        private class SegmentedPackage
        {
            public int TotalLength { get; set; }
            public byte[] Payload { get; set; }
            public int BytesWritten { get; set; }
        }
    }
}
