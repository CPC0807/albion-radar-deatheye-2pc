using Albion.Network;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace VRise.Radar.Packets.Photon
{
    /// <summary>
    /// Custom IPhotonReceiver that uses Protocol 18 deserialization
    /// and creates Albion.Network packet objects directly
    /// </summary>
    public class Protocol18PhotonReceiver : IPhotonReceiver
    {
        private readonly IPhotonReceiver innerReceiver;
        private readonly Type eventPacketType;
        private readonly Type requestPacketType;
        private readonly Type responsePacketType;

        public Protocol18PhotonReceiver(IPhotonReceiver innerReceiver)
        {
            this.innerReceiver = innerReceiver ?? throw new ArgumentNullException(nameof(innerReceiver));

            // Get packet types from Albion.Network assembly
            var assembly = typeof(IPhotonReceiver).Assembly;
            eventPacketType = assembly.GetType("Albion.Network.EventPacket");
            requestPacketType = assembly.GetType("Albion.Network.RequestPacket");
            responsePacketType = assembly.GetType("Albion.Network.ResponsePacket");

            if (eventPacketType == null || requestPacketType == null || responsePacketType == null)
            {
                throw new Exception("Failed to load packet types from Albion.Network");
            }
        }

        public void ReceivePacket(byte[] packet)
        {
            if (packet == null || packet.Length < 1)
                return;

            try
            {
                byte msgType = packet[0];
                var data = new byte[packet.Length - 1];
                Array.Copy(packet, 1, data, 0, data.Length);

                switch (msgType)
                {
                    case 2: // Request
                        HandleRequest(data);
                        break;
                    case 3: // Response
                    case 7: // Response Alt
                        HandleResponse(data);
                        break;
                    case 4: // Event
                        HandleEvent(data);
                        break;
                    default:
                        Console.WriteLine($"[Protocol18PhotonReceiver] Unknown message type: {msgType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Protocol18PhotonReceiver] Error: {ex.Message}");
            }
        }

        private void HandleRequest(byte[] data)
        {
            if (data.Length < 1)
                return;

            byte opCode = data[0];
            var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, 1));

            if (!parameters.ContainsKey(253))
            {
                parameters[253] = (int)opCode;
            }

            // Create RequestPacket using reflection
            var packet = Activator.CreateInstance(requestPacketType);
            SetProperty(packet, "Parameters", parameters);

            // Pass to inner receiver's handlers
            innerReceiver.ReceivePacket(SerializePacketBack(2, opCode, parameters));
        }

        private void HandleResponse(byte[] data)
        {
            if (data.Length < 3)
                return;

            byte opCode = data[0];
            short returnCode = (short)(data[1] | (data[2] << 8));

            int offset = 3;
            string debugMessage = "";

            // Skip debug message if present
            if (offset < data.Length)
            {
                offset++; // skip debug type byte
                // We'll skip parsing debug message for simplicity
            }

            var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, offset));

            if (!parameters.ContainsKey(253))
            {
                parameters[253] = (int)opCode;
            }

            // Pass to inner receiver's handlers
            innerReceiver.ReceivePacket(SerializePacketBack(3, opCode, parameters));
        }

        private void HandleEvent(byte[] data)
        {
            if (data.Length < 1)
                return;

            byte code = data[0];
            var parameters = Protocol18Deserializer.DeserializeParameterTable(SubArray(data, 1));

            if (!parameters.ContainsKey(252))
            {
                parameters[252] = (int)code;
            }

            Console.WriteLine($"[Protocol18PhotonReceiver] Event {code} with {parameters.Count} parameters");

            // Pass to inner receiver's handlers
            innerReceiver.ReceivePacket(SerializePacketBack(4, code, parameters));
        }

        private byte[] SerializePacketBack(byte msgType, byte code, Dictionary<byte, object> parameters)
        {
            // Create a minimal Protocol 16-style packet that Albion.Network can parse
            var result = new List<byte>();
            result.Add(msgType);
            result.Add(code);

            // Parameter count
            result.Add((byte)parameters.Count);

            foreach (var kvp in parameters)
            {
                result.Add(kvp.Key);
                result.AddRange(SerializeValue(kvp.Value));
            }

            return result.ToArray();
        }

        private byte[] SerializeValue(object value)
        {
            var result = new List<byte>();

            if (value == null)
            {
                result.Add(42); // Null type
                return result.ToArray();
            }

            Type t = value.GetType();

            if (t == typeof(int))
            {
                result.Add(105); // Int32 type
                result.AddRange(BitConverter.GetBytes((int)value));
            }
            else if (t == typeof(byte))
            {
                result.Add(3); // Byte type
                result.Add((byte)value);
            }
            else if (t == typeof(byte[]))
            {
                byte[] arr = (byte[])value;
                result.Add(120); // ByteArray type
                result.AddRange(BitConverter.GetBytes(arr.Length));
                result.AddRange(arr);
            }
            else if (t == typeof(string))
            {
                string str = (string)value;
                result.Add(115); // String type
                var bytes = System.Text.Encoding.UTF8.GetBytes(str);
                result.AddRange(BitConverter.GetBytes((short)bytes.Length));
                result.AddRange(bytes);
            }
            else
            {
                // Default to null for unknown types
                result.Add(42);
            }

            return result.ToArray();
        }

        private void SetProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
        }

        private byte[] SubArray(byte[] data, int start)
        {
            if (start >= data.Length)
                return new byte[0];

            var result = new byte[data.Length - start];
            Array.Copy(data, start, result, 0, result.Length);
            return result;
        }
    }
}
