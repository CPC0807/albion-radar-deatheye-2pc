using VRise.Settings;
using PrecisionTiming;
using VRise.Radar.GameObjects.Mobs;
using VRise.Radar.GameObjects.Players;
using VRise.Radar.GameObjects.LocalPlayer;
using System.Reflection;

namespace VRise.Radar
{
    [Obfuscation(Feature = "mutation", Exclude = false)]
    public class GlobalTimer
    {
        private static PrecisionTimer localPlayerTimer, playersTimer, mobsTimer, secTimer;

        private readonly LocalPlayerHandler localPlayerHandler;
        private readonly PlayersHandler playersHandler;
        private readonly MobsHandler mobsHandler;

        public GlobalTimer(LocalPlayerHandler localPlayerHandler, PlayersHandler playersHandler, MobsHandler mobsHandler)
        {
            this.localPlayerHandler = localPlayerHandler;
            this.playersHandler = playersHandler;
            this.mobsHandler = mobsHandler;

            localPlayerTimer = new PrecisionTimer();
            playersTimer = new PrecisionTimer();
            mobsTimer = new PrecisionTimer();
            secTimer = new PrecisionTimer();
        }

        public void Start()
        {
            localPlayerTimer.SetInterval(localPlayerHandler.SyncPosition, 7);
            playersTimer.SetInterval(playersHandler.SyncPlayersPosition, 7);
            mobsTimer.SetInterval(mobsHandler.SyncMobsPositions, 7);
            secTimer.SetInterval(Update, 1000);
        }

        private void Update()
        {
            playersHandler.RegenerateHealth();
        }
    }
}
