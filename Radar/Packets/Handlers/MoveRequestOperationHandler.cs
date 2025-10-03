using X975.Radar.GameObjects.LocalPlayer;
using X975.Radar.GameObjects.Harvestables;
using X975.Radar.GameObjects.Players;
using Albion.Network;
using System.Threading.Tasks;
using System;

namespace X975.Radar.Packets.Handlers
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

            // 存儲本地玩家座標，用於反推 XorCode
            PlayersHandler.LocalPlayerPosition = value.Position;

            #if DEBUG
            Console.WriteLine($"\n[LocalPlayer Position] X:{value.Position.X:F2} Y:{value.Position.Y:F2}");
            #endif

            if(!localPlayerHandler.localPlayer.IsStanding)
                harvestablesHandler.RemoveHarvestables();

            return Task.CompletedTask;
        }
    }
}
