using Albion.Network;
using PacketDotNet;
using SharpPcap;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using SharpPcap.LibPcap;
using VRise.Radar.Packets.Photon;
using VRise.Tools;

namespace VRise.Radar.Sniffer
{
    [Obfuscation(Feature = "mutation", Exclude = false)]
    public class PacketDeviceSelector
    {
        private readonly IPhotonReceiver photonReceiver;
        private readonly int gamePort;
        private PhotonParser photonParser;
        private static bool _debuggedEventConstructors = false;
        private static bool _debuggedRequestConstructors = false;
        private static bool _debuggedResponseConstructors = false;

        public PacketDeviceSelector(IPhotonReceiver photonReceiver, int gamePort = 5050)
        {
            this.photonReceiver = photonReceiver;
            this.gamePort = gamePort;
            this.photonParser = new PhotonParser(photonReceiver);

            Console.WriteLine("[PacketDeviceSelector] Using Protocol 18 PhotonParser");
        }

        public void Start()
        {
            try
            {
                var devices = CaptureDeviceList.Instance;

                if (devices.Count <= 0)
                {
                    if (System.Globalization.CultureInfo.CurrentCulture.ToString() == "ru-RU")
                    {
                        MessageBox.Show("Ошибка! \nНету доступных адаптеров для прослушки!");
                        Environment.Exit(0);
                    }
                    else
                    {
                        MessageBox.Show("Error! \nThere are no listening adapters available!");
                        Environment.Exit(0);
                    }
                }

                foreach (ILiveDevice device in devices)
                {
                   if (device.MacAddress != null ||
                       // capture loopback too
                       ((device as LibPcapLiveDevice)?.Addresses?.Any(e => e.Addr.ipAddress?.Equals(IPAddress.Loopback) ?? false) ?? false))
                   {
                       PacketEvent(device);
                   }
                }
            }
            catch (Exception e)
            {
                if (System.Globalization.CultureInfo.CurrentCulture.ToString() == "ru-RU")
                {
                    MessageBox.Show("Установи NPCAP \nНе трогай галки при установке!");
                    Environment.Exit(0);
                }
                else
                {
                    MessageBox.Show("Install NPCAP \nDon't change the checkboxes!");
                    Environment.Exit(0);
                }
            }
        }

        private void PacketEvent(ICaptureDevice device)
        {
            if (!device.Started)
            {
                device.Open(new DeviceConfiguration()
                {
                    Mode = DeviceModes.DataTransferUdp | DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness,
                    ReadTimeout = 5
                });

                device.Filter = $"udp and port {gamePort}";
                device.OnPacketArrival += Device_OnPacketArrival;
                device.StartCapture();
            }
        }

        private Protocol18ReceiverBridge protocol18Bridge;

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            try
            {
                var packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data).Extract<UdpPacket>();

                if (packet != null && packet.PayloadData != null && packet.PayloadData.Length > 0)
                {
                    // Use our custom Protocol 18 PhotonParser instead of Albion.Network's Protocol 16 parser
                    photonParser.ReceivePacket(packet.PayloadData);
                    return;

                    /* DISABLED: Old Protocol18Bridge approach with reflection
                    // Initialize bridge once
                    if (protocol18Bridge == null)
                    {
                        protocol18Bridge = new Protocol18ReceiverBridge(photonReceiver);

                        // Set up callbacks to invoke handlers using reflection
                        protocol18Bridge.OnEvent = (code, parameters) =>
                        {
                            Console.WriteLine($"[Protocol18Bridge] Event {code} with {parameters.Count} parameters");

                            try
                            {
                                var assembly = typeof(IPhotonReceiver).Assembly;
                                var eventPacketType = assembly.GetType("Albion.Network.EventPacket");
                                if (eventPacketType != null)
                                {
                                    // Debug: Print available constructors (only once)
                                    if (!_debuggedEventConstructors)
                                    {
                                        Console.WriteLine("[DEBUG] EventPacket constructors:");
                                        foreach (var ctor in eventPacketType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                                        {
                                            var ps = ctor.GetParameters();
                                            Console.WriteLine($"  Constructor({string.Join(", ", ps.Select(p => p.ParameterType.Name + " " + p.Name))})");
                                        }
                                        _debuggedEventConstructors = true;
                                    }

                                    // Try different constructor signatures
                                    object eventPacket = null;

                                    try
                                    {
                                        // Try: EventPacket(Dictionary<byte, object> parameters)
                                        eventPacket = Activator.CreateInstance(eventPacketType, parameters);
                                    }
                                    catch (Exception ex1)
                                    {
                                        Console.WriteLine($"[DEBUG] EventPacket(params) failed: {ex1.Message}");
                                        try
                                        {
                                            // Try: EventPacket() with Parameters property setter
                                            eventPacket = Activator.CreateInstance(eventPacketType);
                                            var parametersProperty = eventPacketType.GetProperty("Parameters");
                                            if (parametersProperty != null && parametersProperty.CanWrite)
                                            {
                                                parametersProperty.SetValue(eventPacket, parameters);
                                            }
                                        }
                                        catch (Exception ex2)
                                        {
                                            Console.WriteLine($"[DEBUG] EventPacket() failed: {ex2.Message}");
                                            // Try: EventPacket() with Parameters field
                                            eventPacket = Activator.CreateInstance(eventPacketType);
                                            var parametersField = eventPacketType.GetField("Parameters",
                                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                            if (parametersField != null)
                                            {
                                                parametersField.SetValue(eventPacket, parameters);
                                            }
                                        }
                                    }

                                    if (eventPacket != null)
                                    {
                                        // Invoke ReceiveEvent method
                                        var receiveEventMethod = photonReceiver.GetType().GetMethod("ReceiveEvent",
                                            BindingFlags.NonPublic | BindingFlags.Instance);

                                        if (receiveEventMethod != null)
                                        {
                                            receiveEventMethod.Invoke(photonReceiver, new[] { eventPacket });
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Protocol18Bridge] Error invoking event handler: {ex.Message}");
                            }
                        };

                        protocol18Bridge.OnRequest = (opCode, parameters) =>
                        {
                            Console.WriteLine($"[Protocol18Bridge] Request {opCode} with {parameters.Count} parameters");

                            try
                            {
                                var assembly = typeof(IPhotonReceiver).Assembly;
                                var requestPacketType = assembly.GetType("Albion.Network.RequestPacket");
                                if (requestPacketType != null)
                                {
                                    object requestPacket = null;

                                    try
                                    {
                                        requestPacket = Activator.CreateInstance(requestPacketType, parameters);
                                    }
                                    catch
                                    {
                                        try
                                        {
                                            requestPacket = Activator.CreateInstance(requestPacketType);
                                            var parametersProperty = requestPacketType.GetProperty("Parameters");
                                            if (parametersProperty != null && parametersProperty.CanWrite)
                                            {
                                                parametersProperty.SetValue(requestPacket, parameters);
                                            }
                                        }
                                        catch
                                        {
                                            requestPacket = Activator.CreateInstance(requestPacketType);
                                            var parametersField = requestPacketType.GetField("Parameters",
                                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                            if (parametersField != null)
                                            {
                                                parametersField.SetValue(requestPacket, parameters);
                                            }
                                        }
                                    }

                                    if (requestPacket != null)
                                    {
                                        var receiveRequestMethod = photonReceiver.GetType().GetMethod("ReceiveRequest",
                                            BindingFlags.NonPublic | BindingFlags.Instance);

                                        if (receiveRequestMethod != null)
                                        {
                                            receiveRequestMethod.Invoke(photonReceiver, new[] { requestPacket });
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Protocol18Bridge] Error invoking request handler: {ex.Message}");
                            }
                        };

                        protocol18Bridge.OnResponse = (opCode, returnCode, debugMsg, parameters) =>
                        {
                            Console.WriteLine($"[Protocol18Bridge] Response {opCode} (rc={returnCode}) with {parameters.Count} parameters");

                            try
                            {
                                var assembly = typeof(IPhotonReceiver).Assembly;
                                var responsePacketType = assembly.GetType("Albion.Network.ResponsePacket");
                                if (responsePacketType != null)
                                {
                                    // Debug: Print available constructors (only once)
                                    if (!_debuggedResponseConstructors)
                                    {
                                        Console.WriteLine("[DEBUG] ResponsePacket constructors:");
                                        foreach (var ctor in responsePacketType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                                        {
                                            var ps = ctor.GetParameters();
                                            Console.WriteLine($"  Constructor({string.Join(", ", ps.Select(p => p.ParameterType.Name + " " + p.Name))})");
                                        }
                                        _debuggedResponseConstructors = true;
                                    }

                                    object responsePacket = null;

                                    try
                                    {
                                        responsePacket = Activator.CreateInstance(responsePacketType, parameters);
                                    }
                                    catch (Exception ex1)
                                    {
                                        Console.WriteLine($"[DEBUG] ResponsePacket(params) failed: {ex1.Message}");
                                        try
                                        {
                                            responsePacket = Activator.CreateInstance(responsePacketType);
                                            var parametersProperty = responsePacketType.GetProperty("Parameters");
                                            if (parametersProperty != null && parametersProperty.CanWrite)
                                            {
                                                parametersProperty.SetValue(responsePacket, parameters);
                                            }
                                        }
                                        catch (Exception ex2)
                                        {
                                            Console.WriteLine($"[DEBUG] ResponsePacket() failed: {ex2.Message}");
                                            responsePacket = Activator.CreateInstance(responsePacketType);
                                            var parametersField = responsePacketType.GetField("Parameters",
                                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                            if (parametersField != null)
                                            {
                                                parametersField.SetValue(responsePacket, parameters);
                                            }
                                        }
                                    }

                                    if (responsePacket != null)
                                    {
                                        var receiveResponseMethod = photonReceiver.GetType().GetMethod("ReceiveResponse",
                                            BindingFlags.NonPublic | BindingFlags.Instance);

                                        if (receiveResponseMethod != null)
                                        {
                                            receiveResponseMethod.Invoke(photonReceiver, new[] { responsePacket });
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Protocol18Bridge] Error invoking response handler: {ex.Message}");
                            }
                        };

                        protocol18Bridge.OnEncrypted = () =>
                        {
                            Console.WriteLine($"[Protocol18Bridge] Packet is encrypted");
                        };
                    }

                    protocol18Bridge.ReceivePacket(packet.PayloadData);
                    */ // End of DISABLED code
                }
            }
            // Catch EVERYTHING. This callback runs on the libpcap capture thread;
            // any escaping exception terminates packet capture for the whole session.
            catch (Exception ex)
            {
                ParseErrorLogger.Log("PacketDeviceSelector.OnPacketArrival", ex);
            }
        }
    }
}
