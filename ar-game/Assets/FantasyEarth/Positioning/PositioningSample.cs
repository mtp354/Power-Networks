namespace FantasyEarth.Positioning
{
    public readonly struct PositioningSample
    {
        public PositioningSample(bool isTracking, double horizontalAccuracyM, double yawAccuracyDeg)
        {
            IsTracking = isTracking;
            HorizontalAccuracyM = horizontalAccuracyM;
            YawAccuracyDeg = yawAccuracyDeg;
        }

        public bool IsTracking { get; }
        public double HorizontalAccuracyM { get; }
        public double YawAccuracyDeg { get; }

        public static PositioningSample FromEarth(bool earthEnabled, bool earthTracking,
            double horizontalAccuracyM, double yawAccuracyDeg)
        {
            return new PositioningSample(earthEnabled && earthTracking, horizontalAccuracyM, yawAccuracyDeg);
        }
    }
}
