using Albion.Network;
using VRise.Radar.GameObjects.Mobs;
using System.Threading.Tasks;

namespace VRise.Radar.Packets.Handlers
{
    class NewMobEventHandler : EventPacketHandler<NewMobEvent>
    {
        private readonly MobsHandler mobHandler;

        public NewMobEventHandler(MobsHandler mobHandler) : base(Init.PacketIndexes.NewMobEvent)
        {
            this.mobHandler = mobHandler;
        }

        protected override Task OnActionAsync(NewMobEvent value)
        {
            mobHandler.AddMob(value.Id, value.RawTypeId, value.TypeId, value.Position, value.Health, value.Charge);

            return Task.CompletedTask;
        }
    }
}
