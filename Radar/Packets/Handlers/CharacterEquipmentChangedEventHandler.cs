using Albion.Network;
using VRise.Radar.GameObjects.Players;
using System.Threading.Tasks;

namespace VRise.Radar.Packets.Handlers
{
    class CharacterEquipmentChangedEventHandler : EventPacketHandler<CharacterEquipmentChanged>
    {
        private readonly PlayersHandler playerHandler;

        public CharacterEquipmentChangedEventHandler(PlayersHandler playerHandler) : base(Init.PacketIndexes.CharacterEquipmentChanged)
        {
            this.playerHandler = playerHandler;
        }

        protected override Task OnActionAsync(CharacterEquipmentChanged value)
        {
            playerHandler.UpdateItems(value.Id, value.Equipments, value.Spells);

            return Task.CompletedTask;
        }
    }
}
