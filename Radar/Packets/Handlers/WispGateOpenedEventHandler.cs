using Albion.Network;
using System.Threading.Tasks;
using VRise.Radar.GameObjects.GatedWisps;

namespace VRise.Radar.Packets.Handlers
{
    public class WispGateOpenedEventHandler : EventPacketHandler<WispGateOpenedEvent>
    {
        private readonly GatedWispsHandler wispInGateHandler;

        public WispGateOpenedEventHandler(GatedWispsHandler wispInGateHandler) : base(Init.PacketIndexes.WispGateOpened)
        {
            this.wispInGateHandler = wispInGateHandler;
        }

        protected override Task OnActionAsync(WispGateOpenedEvent value)
        {
            if (value.isCollected)
            {
                wispInGateHandler.Remove(value.Id);
            }

            return Task.CompletedTask;
        }
    }
}
