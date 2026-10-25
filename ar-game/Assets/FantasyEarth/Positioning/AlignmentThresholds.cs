namespace FantasyEarth.Positioning
{
    public sealed class AlignmentThresholds
    {
        public double EnterHorizontalM = 1.5;
        public double EnterYawDeg = 2.0;
        public float EnterDwellS = 1.0f;
        public double ExitHorizontalM = 3.0;
        public double ExitYawDeg = 4.0;
        public float ExitDwellS = 0.5f;
        public float BlendDurationS = 0.75f;

        public static AlignmentThresholds Default => new AlignmentThresholds();
    }
}
