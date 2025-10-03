using Albion.Network;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VRise.Radar.Packets.Handlers
{
    /// <summary>
    /// 調試用：監聽所有事件，尋找可能包含XorCode的封包
    /// </summary>
    public class DebugAllEventsHandler : PacketHandler<object>
    {
        private static HashSet<short> loggedEventCodes = new HashSet<short>();

        protected override Task OnHandleAsync(object packet)
        {
            if (packet is EventPacket eventPacket)
            {
                byte eventCode = eventPacket.Code;
                var parameters = eventPacket.Parameters;

                // 尋找可能包含8-byte數組的參數（XorCode應該是8 bytes）
                foreach (var kvp in parameters)
                {
                    if (kvp.Value is byte[] byteArray)
                    {
                        // 只記錄8-byte的數組（XorCode的典型長度）
                        if (byteArray.Length == 8)
                        {
                            if (!loggedEventCodes.Contains((short)eventCode))
                            {
                                loggedEventCodes.Add((short)eventCode);
                                Console.WriteLine($"[EventScan] EventCode:{eventCode} has 8-byte array at key:{kvp.Key} Value:{BitConverter.ToString(byteArray)}");
                            }
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
