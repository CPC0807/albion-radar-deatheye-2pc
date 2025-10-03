using Albion.Network;
using VRise.Radar.GameObjects.Players;
using System.Threading.Tasks;

namespace VRise.Radar.Packets.Handlers
{
    class RegenerationChangedEventHandler : EventPacketHandler<RegenerationChangedEvent>
    {
        private readonly PlayersHandler playerHandler;

        public RegenerationChangedEventHandler(PlayersHandler playerHandler) : base(Init.PacketIndexes.RegenerationHealthChangedEvent)
        {
            this.playerHandler = playerHandler;
        }

        protected override Task OnActionAsync(RegenerationChangedEvent value)
        {
            playerHandler.SetRegeneration(value.Id, value.Health);

            return Task.CompletedTask;
        }
    }
}
