using VRise.Settings;
using VRise.Radar.Drawing.OverlaySettings;
using VRise.Radar.Utility;
using System.Numerics;
using GameOverlay.Drawing;
using System;
using System.Threading.Tasks;
using VRise.Radar.GameObjects.GatedWisps;
using System.Linq;
using VRise.Radar.GameObjects.LocalPlayer;
using VRise.Radar.GameObjects.Players;

namespace VRise.Radar.Drawers
{
    public class GatedWispsDrawerer : IDrawerer
    {
        private readonly ConfigHandler configHandler = ConfigHandler.Source;
        private readonly RadarOverlayBrushesDictionary brushesDictionary;
        private readonly Graphics gfx;

        private readonly LocalPlayerHandler localPlayerHandler;
        private readonly GatedWispsHandler wispInGateHandler;

        public GatedWispsDrawerer(Graphics gfx, RadarOverlayBrushesDictionary brushesDictionary, LocalPlayerHandler localPlayerHandler, GatedWispsHandler wispInGateHandler)
        {
            this.gfx = gfx;
            this.brushesDictionary = brushesDictionary;

            this.localPlayerHandler = localPlayerHandler;
            this.wispInGateHandler = wispInGateHandler;
        }

        public async Task DrawAsync()
        {
            if (Convert.ToBoolean(configHandler.config.MistWisps[2]))
            {
                lock (wispInGateHandler.gatedWispsList)
                {
                    foreach (GatedWisp w in wispInGateHandler.gatedWispsList.Values)
                    {
                        Vector2 pos = (w.Position - localPlayerHandler.localPlayer.Position).Rotate();

                        gfx.DrawIconDot(brushesDictionary._brushes["Black"], brushesDictionary._mobsImages["MIST_GATE"], pos, Convert.ToSingle(configHandler.config.MistWisps[1]));
                    }
                }
            }
        }
    }
}
