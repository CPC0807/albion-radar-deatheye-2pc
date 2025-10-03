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
        private static System.Collections.Generic.HashSet<int> loggedResponses = new System.Collections.Generic.HashSet<int>();
        private static System.Collections.Generic.HashSet<int> loggedRequests = new System.Collections.Generic.HashSet<int>();
        private static StreamWriter eventLogFile;

        // Request 369 tracking
        private static bool waitingForResponse369 = false;
        private static DateTime lastRequest369Time = DateTime.MinValue;

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
                    int responseCode = Convert.ToInt32(code);

                    // 檢查是否為 Request 369 的 Response
                    if (waitingForResponse369 && (DateTime.UtcNow - lastRequest369Time).TotalSeconds < 2)
                    {
                        Console.WriteLine($"!!! RESPONSE TO REQUEST 369 DETECTED - Response Code: {responseCode} !!!");
                        Console.WriteLine($"    Time since Request 369: {(DateTime.UtcNow - lastRequest369Time).TotalMilliseconds}ms");

                        // 檢查所有 8-byte 陣列
                        foreach (var kvp in response.Parameters)
                        {
                            if (kvp.Value is byte[] byteArray && byteArray.Length == 8)
                            {
                                Console.WriteLine($"    !!! FOUND 8-BYTE ARRAY in Response {responseCode} Key {kvp.Key} !!!");
                                Console.WriteLine($"        Hex: {BitConverter.ToString(byteArray)}");

                                // 測試是否為 XorCode
                                if (GameObjects.Players.PlayersHandler.LocalPlayerPosition != default)
                                {
                                    try
                                    {
                                        var localPos = GameObjects.Players.PlayersHandler.LocalPlayerPosition;
                                        //Console.WriteLine($"        Testing as XorCode against LocalPos ({localPos.X:F2}, {localPos.Y:F2})");

                                        // 如果有測試用的加密座標，這裡可以測試解密
                                        // 暫時只記錄，需要在有加密座標時才能測試
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"        XorCode test error: {ex.Message}");
                                    }
                                }
                            }
                        }

                        waitingForResponse369 = false;
                    }

                    // 特別標記 Response 2 (JoinResponse)
                    if (responseCode == 2)
                    {
                        Console.WriteLine("!!! RESPONSE 2 (JoinResponse) CAPTURED !!!");
                    }

                    Console.WriteLine("Response: " + responseCode);

                    // 詳細記錄 Response 結構（和 Event 一樣）
                    if (!loggedResponses.Contains(responseCode))
                    {
                        loggedResponses.Add(responseCode);

                        if (eventLogFile != null)
                        {
                            eventLogFile.WriteLine($"\n[ResponseStructure] Response {responseCode}:");
                            Console.WriteLine($"[ResponseStructure] Response {responseCode} logged");

                            foreach (var kvp in response.Parameters)
                            {
                                string valueInfo = "";
                                if (kvp.Value is byte[] byteArray)
                                {
                                    valueInfo = $"byte[{byteArray.Length}] = {BitConverter.ToString(byteArray.Take(Math.Min(16, byteArray.Length)).ToArray())}...";

                                    // 標記 byte 陣列
                                    if (byteArray.Length > 0 && byteArray.Any(b => b != 0))
                                    {
                                        string marker = "";
                                        if (byteArray.Length == 8)
                                            marker = "<<< 8-BYTE ARRAY >>>";
                                        else if (byteArray.Length >= 4 && byteArray.Length <= 32)
                                            marker = "<<< POTENTIAL KEY >>>";

                                        if (!string.IsNullOrEmpty(marker))
                                        {
                                            Console.WriteLine($"  {marker} Response {responseCode} Key {kvp.Key} - byte[{byteArray.Length}]");
                                        }
                                    }
                                }
                                else if (kvp.Value != null)
                                {
                                    valueInfo = $"{kvp.Value.GetType().Name} = {kvp.Value}";
                                }
                                eventLogFile.WriteLine($"  Key {kvp.Key}: {valueInfo}");
                            }

                            // 如果是 Response 2，額外檢查隱藏參數
                            if (responseCode == 2)
                            {
                                eventLogFile.WriteLine("  --- SCANNING ALL PARAMETER KEYS FOR HIDDEN DATA ---");
                                for (byte key = 0; key < 255; key++)
                                {
                                    if (response.Parameters.ContainsKey(key) && !response.Parameters.TryGetValue(key, out var _))
                                    {
                                        eventLogFile.WriteLine($"  Hidden Key {key} detected!");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (packet is RequestPacket request)
            {
                if (request.Parameters.TryGetValue(253, out var code))
                {
                    int requestCode = Convert.ToInt32(code);
                    Console.WriteLine("Request: " + requestCode);

                    // 特別標記 Request 369 (MoveRequest)
                    if (requestCode == 369)
                    {
                        var timeSinceLastRequest = (DateTime.UtcNow - lastRequest369Time).TotalMilliseconds;
                        Console.WriteLine($"!!! REQUEST 369 (MoveRequest) - Time since last: {timeSinceLastRequest}ms !!!");

                        // 標記等待 Response
                        waitingForResponse369 = true;
                        lastRequest369Time = DateTime.UtcNow;

                        // 檢查連續的 Request 369（可能觸發 XorCode 更新）
                        if (timeSinceLastRequest < 1000)
                        {
                            Console.WriteLine("    >>> CONSECUTIVE REQUEST 369 DETECTED - XorCode may change! <<<");
                        }
                    }

                    // 也記錄 Request 結構（完整性）
                    if (!loggedRequests.Contains(requestCode))
                    {
                        loggedRequests.Add(requestCode);

                        if (eventLogFile != null)
                        {
                            eventLogFile.WriteLine($"\n[RequestStructure] Request {requestCode}:");
                            Console.WriteLine($"[RequestStructure] Request {requestCode} logged");

                            foreach (var kvp in request.Parameters)
                            {
                                string valueInfo = "";
                                if (kvp.Value is byte[] byteArray)
                                {
                                    valueInfo = $"byte[{byteArray.Length}] = {BitConverter.ToString(byteArray.Take(Math.Min(16, byteArray.Length)).ToArray())}...";

                                    if (byteArray.Length > 0 && byteArray.Any(b => b != 0))
                                    {
                                        string marker = "";
                                        if (byteArray.Length == 8)
                                            marker = "<<< 8-BYTE ARRAY >>>";
                                        else if (byteArray.Length >= 4 && byteArray.Length <= 32)
                                            marker = "<<< POTENTIAL KEY >>>";

                                        if (!string.IsNullOrEmpty(marker))
                                        {
                                            Console.WriteLine($"  {marker} Request {requestCode} Key {kvp.Key} - byte[{byteArray.Length}]");
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