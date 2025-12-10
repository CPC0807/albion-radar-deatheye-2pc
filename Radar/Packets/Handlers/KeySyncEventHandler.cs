using VRise.Radar.GameObjects.Players;
using Albion.Network;
using System.Threading.Tasks;

namespace VRise.Radar.Packets.Handlers
{
    public class KeySyncEventHandler : EventPacketHandler<KeySyncEvent>
    {
        private readonly PlayersHandler playersHandler;

        public KeySyncEventHandler(PlayersHandler playersHandler) : base(Init.PacketIndexes.KeySync)
        {
            this.playersHandler = playersHandler;
        }

        protected override Task OnActionAsync(KeySyncEvent value)
        {
            playersHandler.XorCode = value.Code;

            #if DEBUG
            // KeySync 訊息（已註解避免控制台太亂）
            // if (value.Code != null)
            // {
            //     System.Console.WriteLine($"[KeySync] XorCode received! Length:{value.Code.Length} Bytes:{System.BitConverter.ToString(value.Code)}");
            // }
            // else
            // {
            //     System.Console.WriteLine($"[KeySync] XorCode is NULL!");
            // }
            #endif

            return Task.CompletedTask;
        }
    }
}
