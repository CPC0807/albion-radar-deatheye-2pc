using Albion.Network;
using VRise.Radar.GameObjects.Harvestables;
using System.Threading.Tasks;

namespace VRise.Radar.Packets.Handlers
{
    public class HarvestableChangeStateEventHandler : EventPacketHandler<HarvestableChangeStateEvent>
    {
        private readonly HarvestablesHandler harvestableHandler;

        public HarvestableChangeStateEventHandler(HarvestablesHandler harvestableHandler) : base(Init.PacketIndexes.HarvestableChangeState)
        {
            this.harvestableHandler = harvestableHandler;
        }

        protected override Task OnActionAsync(HarvestableChangeStateEvent value)
        {
            harvestableHandler.UpdateHarvestable(value.Id, value.Count, value.Charge);

            return Task.CompletedTask;
        }
    }
}
