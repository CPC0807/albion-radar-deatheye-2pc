using Albion.Network;
using VRise.Radar.GameObjects.Players;
using System.Threading.Tasks;
using VRise.Radar.GameObjects.LocalPlayer;

namespace VRise.Radar.Packets.Handlers
{
    class ChangeFlaggingFinishedEventHandler : EventPacketHandler<ChangeFlaggingFinishedEvent>
    {
        private readonly LocalPlayerHandler localPlayerHandler;
        private readonly PlayersHandler playerHandler;

        public ChangeFlaggingFinishedEventHandler(LocalPlayerHandler localPlayerHandler, PlayersHandler playerHandler) : base(Init.PacketIndexes.ChangeFlaggingFinished)
        {
            this.localPlayerHandler = localPlayerHandler;
            this.playerHandler = playerHandler;
        }

        protected override Task OnActionAsync(ChangeFlaggingFinishedEvent value)
        {
            localPlayerHandler.SetFaction(value.Id, value.Faction);
            playerHandler.SetFaction(value.Id, value.Faction);

            return Task.CompletedTask;
        }
    }
}
