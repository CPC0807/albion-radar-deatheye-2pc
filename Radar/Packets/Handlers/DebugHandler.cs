using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Albion.Network;
using VRise.Radar.Packets.Photon;

namespace VRise.Radar.Packets.Handlers
{
    public class DebugHandler : PacketHandler<object>
    {
        private static System.Collections.Generic.HashSet<int> loggedEvents = new System.Collections.Generic.HashSet<int>();
        private static StreamWriter eventLogFile;

        static DebugHandler()
        {
            try
            {
                eventLogFile = new StreamWriter("event_structures.txt", false) { AutoFlush = true };
                eventLogFile.WriteLine($"=== DebugHandler Initialized at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                eventLogFile.WriteLine("Waiting for Albion Online packets...\n");
                Console.WriteLine("[DebugHandler] Initialized - logging to event_structures.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DebugHandler] Failed to create log file: {ex.Message}");
            }
        }

        protected override Task OnHandleAsync(object packet)
        {
            // just for debugging, i will not remove this

            // 診斷：確認 DebugHandler 是否被調用
            Console.WriteLine($"[DebugHandler] OnHandleAsync called - packet type: {packet?.GetType().Name ?? "null"}");

            if (packet is ResponsePacket response)
            {
                // Use safe type conversion with endianness normalization
                if (PhotonProtocolHelper.TryGetOperationCode(response.Parameters, out var opCode))
                {
                    Console.WriteLine($"[DebugHandler] Response: OpCode={opCode}");
                }
                else
                {
                    Console.WriteLine("[DebugHandler] Response: No valid OpCode (253) found");
                }
            }
            else if (packet is RequestPacket request)
            {
                // Use safe type conversion with endianness normalization
                if (PhotonProtocolHelper.TryGetOperationCode(request.Parameters, out var opCode))
                {
                    Console.WriteLine($"[DebugHandler] Request: OpCode={opCode}");
                }
                else
                {
                    Console.WriteLine("[DebugHandler] Request: No valid OpCode (253) found");
                }
            }
            else if (packet is EventPacket @event)
            {
                // Use safe type conversion with endianness normalization
                if (!PhotonProtocolHelper.TryGetEventCode(@event.Parameters, out var eventCode))
                {
                    Console.WriteLine($"[DebugHandler] Event: Could not extract event code (252)");
                    return Task.CompletedTask;
                }

                // 當切換地圖時，詳細記錄所有事件的參數結構
                if (!loggedEvents.Contains((int)eventCode))
                {
                    loggedEvents.Add((int)eventCode);

                    // 列出所有參數的類型和長度（只寫到檔案，不輸出到Console）
                    if (eventLogFile != null)
                    {
                        try
                        {
                            eventLogFile.WriteLine($"\n[EventStructure] Event {eventCode}:");
                            eventLogFile.Flush(); // 強制刷新
                            Console.WriteLine($"[DebugHandler] New Event Code: {eventCode} - Writing to event_structures.txt");

                            foreach (var kvp in @event.Parameters)
                            {
                                string valueInfo = "";
                                if (kvp.Value is byte[] byteArray)
                                {
                                    valueInfo = $"byte[{byteArray.Length}] = {BitConverter.ToString(byteArray.Take(Math.Min(16, byteArray.Length)).ToArray())}...";

                                    // 標記所有非空的 byte 陣列
                                    if (byteArray.Length > 0 && byteArray.Any(b => b != 0))
                                    {
                                        string marker = "";
                                        if (byteArray.Length == 8)
                                            marker = "<<< 8-BYTE ARRAY >>>";
                                        else if (byteArray.Length >= 4 && byteArray.Length <= 32)
                                            marker = "<<< POTENTIAL KEY >>>";

                                        if (!string.IsNullOrEmpty(marker))
                                        {
                                            // Console.WriteLine($"  {marker} Event {eventCode} Key {kvp.Key} - byte[{byteArray.Length}]"); // 註解避免控制台太亂
                                        }
                                    }
                                }
                                else if (kvp.Value != null)
                                {
                                    valueInfo = $"{kvp.Value.GetType().Name} = {kvp.Value}";
                                }
                                eventLogFile.WriteLine($"  Key {kvp.Key}: {valueInfo}");
                            }
                        eventLogFile.Flush(); // 最終刷新確保寫入
                        Console.WriteLine($"[DebugHandler] Event {eventCode} structure written successfully");
                        }
                        catch (Exception writeEx)
                        {
                            Console.WriteLine($"[DebugHandler] Failed to write event {eventCode}: {writeEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DebugHandler] eventLogFile is NULL! Cannot write event {eventCode}");
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}