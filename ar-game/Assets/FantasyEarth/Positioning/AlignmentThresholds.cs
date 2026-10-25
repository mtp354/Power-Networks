namespace FantasyEarth.Positioning
{
    public sealed class AlignmentThresholds
    {
        // S25 field accuracy settled near 1.9 m; retain a margin below the 3 m exit limit.
        public double EnterHorizontalM = 2.0;
        public double EnterYawDeg = 2.0;
        public float EnterDwellS = 1.0f;
        public double ExitHorizontalM = 3.0;
        public double ExitYawDeg = 4.0;
        public float ExitDwellS = 0.5f;
        public float BlendDurationS = 0.75f;

        public static AlignmentThresholds Default => new AlignmentThresholds();
    }
}
