using System;

namespace FantasyEarth.World
{
    public sealed class MapViewport
    {
        private readonly MetricBounds _bounds;
        private readonly PlanarPoint _previewCenter;
        public MapViewport(MetricBounds bounds,PlanarPoint previewCenter)
        { _bounds=bounds; _previewCenter=bounds.Clamp(previewCenter); Center=_previewCenter; }
        public PlanarPoint Center { get; private set; }
        public double ZoomM { get; private set; }=220;
        public bool Following { get; private set; }=true;
        public bool HasPlayer { get; private set; }
        public PlanarPoint Player { get; private set; }
        public double AccuracyM { get; private set; }

        public void UpdatePlayer(PlanarPoint? position,double accuracyM)
        {
            HasPlayer=position.HasValue && _bounds.Contains(position.Value) && PlanarPoint.Finite(accuracyM) && accuracyM>=0 && accuracyM<=250;
            if (!HasPlayer) return;
            Player=position.Value; AccuracyM=accuracyM;
            if (Following) Center=Player;
        }
        public void Pan(double eastM,double northM)
        { Center=_bounds.Clamp(new PlanarPoint(Center.EastM+eastM,Center.NorthM+northM)); Following=false; }
        public void Zoom(double factor)
        {
            if (!PlanarPoint.Finite(factor) || factor<=0) throw new ArgumentOutOfRangeException(nameof(factor));
            ZoomM=Math.Max(50,Math.Min(650,ZoomM*factor));
        }
        public void Recenter() { Following=true; Center=HasPlayer ? Player : _previewCenter; }
    }
}
