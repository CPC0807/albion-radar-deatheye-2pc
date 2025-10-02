using Albion.Network;
using System;
using System.Threading.Tasks;
using X975.Radar.GameObjects.Players;

namespace X975.Radar.Packets.Handlers
{
    public class BruteForceKeySyncHandler : EventPacketHandler<BruteForceKeySyncEvent>
    {
        private readonly PlayersHandler playersHandler;
        private readonly int testEventCode;

        public BruteForceKeySyncHandler(PlayersHandler playersHandler, int eventCode)
            : base(eventCode)
        {
            this.playersHandler = playersHandler;
            this.testEventCode = eventCode;
        }

        protected override Task OnActionAsync(BruteForceKeySyncEvent value)
        {
            if (value.Code != null && value.Code.Length == 8)
            {
                Console.WriteLine($"[FOUND!] Event {testEventCode} has 8-byte code: {BitConverter.ToString(value.Code)}");
                playersHandler.XorCode = value.Code;
            }
            else if (value.Code != null)
            {
                // 調試：顯示找到的非8-byte陣列
                Console.WriteLine($"[DEBUG] Event {testEventCode} has {value.Code.Length}-byte array");
            }
            return Task.CompletedTask;
        }
    }
}
