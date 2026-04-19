using Albion.Network;
using PhotonPackageParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VRise.Tools;

namespace VRise.Radar.Packets.Photon
{
    /// <summary>
    /// Photon Protocol 18 packet wrapper.
    /// Parses raw Photon UDP/TCP payloads using Protocol 18 deserialization,
    /// then dispatches directly to the AlbionParser's OnEvent/OnRequest/OnResponse
    /// via reflection — bypassing Albion.Network's own Protocol 16 byte parser
    /// (which cannot read Protocol 18 data).
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
        // msgType 7 is Photon's InternalOperationResponse (ping/pong, etc.), not an
        // Albion application-level response. Its payload layout differs from MsgResponse,
        // so parsing it as a response produces garbage and EndOfStream on the param table.
        // The reference Go client (albiondata-client) ignores type 7 entirely; we do too.
        private const byte MsgInternalOpResponse = 7;
        private const byte MsgEncrypted = 131;

        // Cap to avoid unbounded growth if fragment reassembly never completes for some sequences.
        private const int MaxPendingSegments = 256;
        private readonly Dictionary<int, SegmentedPackage> pendingSegments = new Dictionary<int, SegmentedPackage>();
        private readonly IPhotonReceiver photonReceiver;

        // We bypass AlbionParser.OnEvent/OnRequest/OnResponse entirely — those call
        // ParseEventCode/ParseOperationCode, which expect parameters[252]/[253] to be
        // an Int16 produced by the built-in Protocol 16 deserializer. Protocol 18's
        // deserializer produces different boxed types, so those casts throw.
        //
        // Instead, we construct EventPacket/RequestPacket/ResponsePacket ourselves and
        // push them straight into AlbionParser.handlers.HandleAsync(), matching what
        // OnEvent/OnRequest/OnResponse do after parsing the code.
        private readonly object handlersCollection;
        private readonly MethodInfo handlersHandleAsyncMethod;
        private readonly ConstructorInfo eventPacketCtor;
        private readonly ConstructorInfo requestPacketCtor;
        private readonly ConstructorInfo responsePacketCtor;

        public Action OnEncrypted { get; set; }

        public PhotonParser(IPhotonReceiver photonReceiver)
        {
            this.photonReceiver = photonReceiver ?? throw new ArgumentNullException(nameof(photonReceiver));

            var parserType = photonReceiver.GetType();
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // Locate AlbionParser.handlers (HandlersCollection) by field type/name.
            FieldInfo handlersField = null;
            for (var t = parserType; t != null && t != typeof(object); t = t.BaseType)
            {
                handlersField = t.GetField("handlers", bindingFlags);
                if (handlersField != null) break;
            }
            if (handlersField == null)
                throw new InvalidOperationException("PhotonParser: AlbionParser.handlers field not found.");

            handlersCollection = handlersField.GetValue(photonReceiver)
                ?? throw new InvalidOperationException("PhotonParser: AlbionParser.handlers is null.");

            handlersHandleAsyncMethod = handlersCollection.GetType().GetMethod(
                "HandleAsync", bindingFlags, null, new[] { typeof(object) }, null);
            if (handlersHandleAsyncMethod == null)
                throw new InvalidOperationException("PhotonParser: HandlersCollection.HandleAsync(object) not found.");

            var asm = typeof(IPhotonReceiver).Assembly;
            var eventPacketType = asm.GetType("Albion.Network.EventPacket")
                ?? throw new InvalidOperationException("Albion.Network.EventPacket not found.");
            var requestPacketType = asm.GetType("Albion.Network.RequestPacket")
                ?? throw new InvalidOperationException("Albion.Network.RequestPacket not found.");
            var responsePacketType = asm.GetType("Albion.Network.ResponsePacket")
                ?? throw new InvalidOperationException("Albion.Network.ResponsePacket not found.");

            var ctorArgs = new[] { typeof(short), typeof(Dictionary<byte, object>) };
            eventPacketCtor = eventPacketType.GetConstructor(ctorArgs)
                ?? throw new InvalidOperationException("EventPacket(short, Dictionary) ctor not found.");
            requestPacketCtor = requestPacketType.GetConstructor(ctorArgs)
                ?? throw new InvalidOperationException("RequestPacket(short, Dictionary) ctor not found.");
            responsePacketCtor = responsePacketType.GetConstructor(ctorArgs)
                ?? throw new InvalidOperationException("ResponsePacket(short, Dictionary) ctor not found.");
        }

        /// <summary>
        /// Processes a raw Photon UDP/TCP payload.
        /// Returns true if the packet header was valid.
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
                    {
                        ParseErrorLogger.LogWarning("PhotonParser",
                            $"HandleCommand failed at command {i} (commandCount={commandCount}, len={payload.Length})");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ParseErrorLogger.Log("PhotonParser.ReceivePacket", ex);
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

            offset++; // signalByte
            byte msgType = src[offset++];
            cmdLen -= 2;

            if (offset + cmdLen > src.Length)
                return;

            if (msgType == MsgEncrypted)
            {
                OnEncrypted?.Invoke();
                return;
            }

            var data = new byte[cmdLen];
            Array.Copy(src, offset, data, 0, cmdLen);

            RecordMsgType(msgType);

            try
            {
                DispatchMessage(msgType, data);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                // Handler/event-constructor threw synchronously through reflection.
                // Logging it here prevents the sniffer thread from dying when a game
                // update changes a packet schema.
                ParseErrorLogger.Log(
                    $"PhotonParser.DispatchMessage (msgType={msgType})",
                    tie.InnerException);
            }
            catch (Exception ex)
            {
                ParseErrorLogger.Log(
                    $"PhotonParser.DispatchMessage (msgType={msgType})",
                    ex);
            }
        }

        /// <summary>
        /// Parses Protocol 18 payload for the given message type, constructs the matching
        /// Albion.Network packet object, and pushes it directly into the handler pipeline —
        /// bypassing AlbionParser.OnEvent/OnRequest/OnResponse (which mis-parse Protocol 18
        /// parameters[252/253] types).
        /// </summary>
        private void DispatchMessage(byte msgType, byte[] data)
        {
            if (msgType == MsgRequest)
            {
                if (data.Length < 1)
                    return;

                byte envelopeByte = data[0];
                var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, 1));

                // The real operation code lives in parameters[253]. data[0] is just the
                // Photon envelope byte (typically 0/1) and is NOT the app-level opCode.
                short opCode = ExtractCode(parameters, 253, envelopeByte);
                DumpParamTypesOnce("Request", (byte)opCode, parameters);

                var packet = requestPacketCtor.Invoke(new object[] { opCode, parameters });
                ObserveHandlerTask(handlersHandleAsyncMethod.Invoke(handlersCollection, new[] { packet }),
                    $"Request OpCode={opCode}", parameters);
            }
            else if (msgType == MsgInternalOpResponse)
            {
                // Photon internal ping/pong. Shape is not an Albion parameter table —
                // silently drop to avoid EndOfStream noise.
                return;
            }
            else if (msgType == MsgResponse)
            {
                if (data.Length < 3)
                    return;

                byte envelopeByte = data[0];

                // ResponsePacket stores only OperationCode and Parameters; returnCode and
                // debugMessage are not exposed on the packet, but we still need to advance
                // past them to reach the parameter table.
                int offset = 3;

                if (offset < data.Length)
                {
                    byte debugType = data[offset++];
                    using (var stream = new MemoryStream(data, offset, data.Length - offset))
                    {
                        try
                        {
                            DeserializeSingle(stream, debugType);
                            offset += (int)stream.Position;
                        }
                        catch
                        {
                            // ignore malformed debug message
                        }
                    }
                }

                var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, offset));
                short opCode = ExtractCode(parameters, 253, envelopeByte);
                DumpParamTypesOnce("Response", (byte)opCode, parameters);

                var packet = responsePacketCtor.Invoke(new object[] { opCode, parameters });
                ObserveHandlerTask(handlersHandleAsyncMethod.Invoke(handlersCollection, new[] { packet }),
                    $"Response OpCode={opCode}", parameters);
            }
            else if (msgType == MsgEvent)
            {
                if (data.Length < 1)
                    return;

                byte envelopeByte = data[0];
                var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, 1));

                // The real event code lives in parameters[252]. data[0] is only the
                // Photon envelope byte — overwriting params[252] with it (as the previous
                // bypass did) destroyed the real code and caused every event to get
                // routed to whatever handler matched data[0] (1=Leave, 3=Move), while
                // mobs / harvestables / etc. never dispatched.
                short eventCode = ExtractCode(parameters, 252, envelopeByte);
                DumpParamTypesOnce("Event", (byte)eventCode, parameters);

                var packet = eventPacketCtor.Invoke(new object[] { eventCode, parameters });
                ObserveHandlerTask(handlersHandleAsyncMethod.Invoke(handlersCollection, new[] { packet }),
                    $"Event code={eventCode}", parameters);
            }
            else
            {
                LogUnknownMsgType(msgType, data);
            }
        }

        // Extracts the real app-level event/operation code from the parameter table.
        // Photon stores it under key 252 (events) / 253 (requests/responses), typed as Int16
        // by the Protocol 18 deserializer. Falls back to the envelope byte if missing or
        // unreadable so we still make progress (and the fallback will simply miss handlers).
        private static short ExtractCode(Dictionary<byte, object> parameters, byte key, byte envelopeByte)
        {
            if (parameters.TryGetValue(key, out var v) && v != null)
            {
                if (v is short s) return s;
                if (v is ushort us) return (short)us;
                if (v is byte b) return b;
                if (v is sbyte sb) return sb;
                if (v is int i) return (short)i;
                if (v is uint ui) return (short)ui;
                if (v is long l) return (short)l;
            }
            return envelopeByte;
        }

        // For each (kind, code) seen for the first time, dump parameter key -> type map so
        // we can identify which Protocol 18 fields are being delivered as typed arrays
        // (byte[], int[], etc.) instead of the scalars that handlers expect.
        private readonly HashSet<string> _dumpedCodes = new HashSet<string>();
        private void DumpParamTypesOnce(string kind, byte code, Dictionary<byte, object> parameters)
        {
            var tag = kind + "/" + code;
            if (!_dumpedCodes.Add(tag)) return;

            var parts = new List<string>();
            foreach (var kvp in parameters.OrderBy(p => p.Key))
            {
                var typeName = kvp.Value?.GetType().Name ?? "null";
                if (kvp.Value is Array arr)
                    typeName += $"[{arr.Length}]";
                parts.Add($"{kvp.Key}:{typeName}");
            }
            Console.WriteLine($"[PhotonParser] First {kind} code={code} params: {string.Join(", ", parts)}");
        }

        private static void ObserveHandlerTask(object taskObj, string tag, Dictionary<byte, object> parameters)
        {
            if (!(taskObj is Task task)) return;

            task.ContinueWith(t =>
            {
                if (t.Exception == null) return;
                try
                {
                    ParseErrorLogger.Log("PhotonParser.Handler (" + tag + ")",
                        t.Exception.InnerException ?? t.Exception, parameters);
                }
                catch
                {
                    // Swallow — logging must never bring down the task scheduler.
                }
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        private readonly HashSet<byte> _loggedUnknownMsgTypes = new HashSet<byte>();
        private void LogUnknownMsgType(byte msgType, byte[] data)
        {
            if (_loggedUnknownMsgTypes.Add(msgType))
            {
                var preview = BitConverter.ToString(data, 0, Math.Min(16, data.Length));
                Console.WriteLine($"[PhotonParser] Unknown msgType={msgType} len={data.Length} first16={preview}");
            }
        }

        // Periodic msgType histogram so we can see whether Responses (type 3 / 7) ever arrive.
        private readonly Dictionary<byte, long> _msgTypeCounts = new Dictionary<byte, long>();
        private long _msgTypeTotal;
        private void RecordMsgType(byte msgType)
        {
            _msgTypeCounts.TryGetValue(msgType, out var count);
            _msgTypeCounts[msgType] = count + 1;
            _msgTypeTotal++;

            // First time each msgType shows up, shout about it.
            if (count == 0)
            {
                Console.WriteLine($"[PhotonParser] First occurrence of msgType={msgType} (total packets so far={_msgTypeTotal})");
            }

            if (_msgTypeTotal % 100 == 0)
            {
                var parts = new List<string>();
                foreach (var kvp in _msgTypeCounts)
                    parts.Add($"{kvp.Key}={kvp.Value}");
                Console.WriteLine($"[PhotonParser] msgType histogram (total={_msgTypeTotal}): {string.Join(", ", parts)}");
            }
        }

        private object DeserializeSingle(MemoryStream stream, byte typeCode)
        {
            // Wrap a single value in a fake 1-entry parameter table so the shared deserializer handles it.
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
            if (offset + 20 > src.Length)
                return offset + cmdLen;

            int startSeq = (src[offset] << 24) | (src[offset + 1] << 16) | (src[offset + 2] << 8) | src[offset + 3];
            int totalLen = (src[offset + 12] << 24) | (src[offset + 13] << 16) | (src[offset + 14] << 8) | src[offset + 15];
            int fragOffset = (src[offset + 16] << 24) | (src[offset + 17] << 16) | (src[offset + 18] << 8) | src[offset + 19];

            offset += 20;
            cmdLen -= 20;

            if (!pendingSegments.TryGetValue(startSeq, out var package))
            {
                if (pendingSegments.Count >= MaxPendingSegments)
                {
                    // Drop the oldest incomplete reassembly buffer to bound memory.
                    var oldestKey = default(int);
                    var first = true;
                    foreach (var key in pendingSegments.Keys)
                    {
                        if (first) { oldestKey = key; first = false; }
                    }
                    pendingSegments.Remove(oldestKey);
                }

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
