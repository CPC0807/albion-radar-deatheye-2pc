using VRise.Radar.GameObjects.LocalPlayer;
using VRise.Radar.GameObjects.Harvestables;
using Albion.Network;
using System.Threading.Tasks;

namespace VRise.Radar.Packets.Handlers
{
    public class MoveRequestOperationHandler : RequestPacketHandler<MoveRequestOperation>
    {
        private readonly LocalPlayerHandler localPlayerHandler;
        private readonly HarvestablesHandler harvestablesHandler;

        public MoveRequestOperationHandler(LocalPlayerHandler localPlayerHandler, HarvestablesHandler harvestablesHandler) : base(Init.PacketIndexes.MoveRequest)
        {
            this.localPlayerHandler = localPlayerHandler;
            this.harvestablesHandler = harvestablesHandler;
        }

        protected override Task OnActionAsync(MoveRequestOperation value)
        {
            localPlayerHandler.Move(value.Position, value.NewPosition, value.Speed, value.Time);
            
            if(!localPlayerHandler.localPlayer.IsStanding)
                harvestablesHandler.RemoveHarvestables();

            return Task.CompletedTask;
        }
    }
}
