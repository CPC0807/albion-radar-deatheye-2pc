using Albion.Network;
using System.Threading.Tasks;
using VRise.Radar.GameObjects.FishNodes;

namespace VRise.Radar.Packets.Handlers
{
    public class NewFishingZoneEventHandler : EventPacketHandler<NewFishingZoneEvent>
    {
        private readonly FishNodesHandler fishZoneHandler;
        public NewFishingZoneEventHandler(FishNodesHandler fishZoneHandler) : base(Init.PacketIndexes.NewFishingZoneObject)
        {
            this.fishZoneHandler = fishZoneHandler;
        }

        protected override Task OnActionAsync(NewFishingZoneEvent value)
        {
            fishZoneHandler.AddFishZone(value.Id, value.Position, value.Size, value.RespawnCount);

            return Task.CompletedTask;
        }
    }
}
