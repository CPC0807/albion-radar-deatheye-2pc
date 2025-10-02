using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Albion.Network;

namespace X975.Radar.Packets.Handlers
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

            if (packet is ResponsePacket response)
            {
                if (response.Parameters.TryGetValue(253, out var code))
                {
                    Console.WriteLine("Response: " + code);
                }
                else
                {
                    ;
                }
            }
            else if (packet is RequestPacket request)
            {
                if (request.Parameters.TryGetValue(253, out var code))
                {
                    Console.WriteLine("Request: " + code);
                }
                else
                {
                    ;
                }
            }
            else if (packet is EventPacket @event)
            {
                if (@event.Parameters.TryGetValue(252, out var code))
                {
                    int eventCode = Convert.ToInt32(code);

                    // 當切換地圖時，詳細記錄所有事件的參數結構
                    if (!loggedEvents.Contains(eventCode))
                    {
                        loggedEvents.Add(eventCode);

                        // 列出所有參數的類型和長度（只寫到檔案，不輸出到Console）
                        if (eventLogFile != null)
                        {
                            eventLogFile.WriteLine($"\n[EventStructure] Event {eventCode}:");
                            Console.WriteLine($"[EventStructure] Event {eventCode} logged");

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
                                            Console.WriteLine($"  {marker} Event {eventCode} Key {kvp.Key} - byte[{byteArray.Length}]");
                                        }
                                    }
                                }
                                else if (kvp.Value != null)
                                {
                                    valueInfo = $"{kvp.Value.GetType().Name} = {kvp.Value}";
                                }
                                eventLogFile.WriteLine($"  Key {kvp.Key}: {valueInfo}");
                            }
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}