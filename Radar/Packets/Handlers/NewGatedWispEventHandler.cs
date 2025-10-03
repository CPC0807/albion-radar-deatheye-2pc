using Albion.Network;
using System.Threading.Tasks;
using VRise.Radar.GameObjects.GatedWisps;

namespace VRise.Radar.Packets.Handlers
{
    public class NewGatedWispEventHandler : EventPacketHandler<NewGatedWispEvent>
    {
        private readonly GatedWispsHandler wispInGateHandler;

        public NewGatedWispEventHandler(GatedWispsHandler wispInGateHandler) : base(Init.PacketIndexes.NewWispGate)
        {
            this.wispInGateHandler = wispInGateHandler;
        }

        protected override Task OnActionAsync(NewGatedWispEvent value)
        {
            if (!value.isCollected)
                wispInGateHandler.AddWispInGate(value.Id, value.Position);

            return Task.CompletedTask;
        }
    }
}
