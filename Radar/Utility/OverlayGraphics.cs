using GameOverlay.Drawing;

namespace VRise.Radar
{
    public class OverlayGraphics : Graphics
    {
        public OverlayGraphics()
        {
            this.MeasureFPS = true;
            this.PerPrimitiveAntiAliasing = true;
            this.TextAntiAliasing = true;
            this.UseMultiThreadedFactories = true;
        }
    }
}
