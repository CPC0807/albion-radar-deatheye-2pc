using VRise.Radar.Utility;
using System.Threading.Tasks;
using VRise.Radar.Drawers;
using VRise.Radar.GameObjects.LocalPlayer;
using VRise.Radar.OverlaySettings;
using VRise.Settings;

namespace VRise.Radar.Drawing.Overlays
{
    public class InfoOverlay : Overlay
    {
        private readonly LocalPlayerHandler localPlayerHandler;
        private readonly InfoOverlayBrushesDictionary infoOverlayBrushesDictionary;
        private readonly IDrawerer infoDrawerer;
        private readonly ConfigHandler configHandler = ConfigHandler.Source;

        public InfoOverlay(LocalPlayerHandler localPlayerHandler)
        {
            FPS = 20;
            IsTopmost = true;
            IsTransparent = true;
            IsVisible = true;
            Width = 500;
            Height = 60;
            X = 0;
            Y = 0;

            this.localPlayerHandler = localPlayerHandler;
            infoOverlayBrushesDictionary = new InfoOverlayBrushesDictionary(Graphics);
            infoDrawerer = new InfoDrawerer(this, infoOverlayBrushesDictionary, this.localPlayerHandler);
        }

        protected override async Task InitGraphics()
        {
            await infoOverlayBrushesDictionary.Init();
        }

        protected override async Task DrawAsync()
        {
            if (localPlayerHandler.localPlayer.CurrentCluster.Subtype != ClusterSubtype.Unknown && configHandler.config.MistOverlayEnabled)
            {
                if (X != configHandler.config.MistOverlayX)
                    X = configHandler.config.MistOverlayX;

                if (Y != configHandler.config.MistOverlayY)
                    Y = configHandler.config.MistOverlayY;


                await infoDrawerer.DrawAsync();
            }
        }
    }
}
