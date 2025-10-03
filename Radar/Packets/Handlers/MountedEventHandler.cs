using Albion.Network;
using VRise.Radar.GameObjects.Players;
using System.Threading.Tasks;

namespace VRise.Radar.Packets.Handlers
{
    class MountedEventHandler : EventPacketHandler<MountedEvent>
    {
        private readonly PlayersHandler playerHandler;

        public MountedEventHandler(PlayersHandler playerHandler) : base(Init.PacketIndexes.Mounted)
        {
            this.playerHandler = playerHandler;
        }

        protected override Task OnActionAsync(MountedEvent value)
        {
            playerHandler.Mounted(value.Id, value.IsMounted);

            return Task.CompletedTask;
        }
    }
}
