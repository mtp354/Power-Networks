using System;

namespace FantasyEarth.Positioning
{
    public enum AlignmentState { Unlocked, Locked }

    public sealed class AlignmentGate
    {
        private const float DwellToleranceS = 1e-4f;
        private readonly AlignmentThresholds _thresholds;
        private float _enterElapsedS;
        private float _exitElapsedS;

        public AlignmentGate(AlignmentThresholds thresholds)
        {
            _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        }

        public AlignmentState State { get; private set; } = AlignmentState.Unlocked;
        public float RebuiltBlend { get; private set; }

        public void Update(PositioningSample sample, float deltaTimeS)
        {
            if (float.IsNaN(deltaTimeS) || float.IsInfinity(deltaTimeS) || deltaTimeS < 0)
            {
                deltaTimeS = 0;
            }

            bool valid = IsAccuracyValid(sample.HorizontalAccuracyM) && IsAccuracyValid(sample.YawAccuracyDeg);
            bool good = sample.IsTracking && valid
                && sample.HorizontalAccuracyM <= _thresholds.EnterHorizontalM
                && sample.YawAccuracyDeg <= _thresholds.EnterYawDeg;
            bool bad = !sample.IsTracking || !valid
                || sample.HorizontalAccuracyM > _thresholds.ExitHorizontalM
                || sample.YawAccuracyDeg > _thresholds.ExitYawDeg;

            if (!sample.IsTracking)
            {
                State = AlignmentState.Unlocked;
                _enterElapsedS = 0;
                _exitElapsedS = 0;
            }
            else if (State == AlignmentState.Unlocked)
            {
                _exitElapsedS = 0;
                _enterElapsedS = good ? _enterElapsedS + deltaTimeS : 0;
                if (good && _enterElapsedS >= _thresholds.EnterDwellS - DwellToleranceS)
                {
                    State = AlignmentState.Locked;
                    _enterElapsedS = 0;
                }
            }
            else
            {
                _enterElapsedS = 0;
                _exitElapsedS = bad ? _exitElapsedS + deltaTimeS : 0;
                if (bad && _exitElapsedS >= _thresholds.ExitDwellS - DwellToleranceS)
                {
                    State = AlignmentState.Unlocked;
                    _exitElapsedS = 0;
                }
            }

            // State changes happen first, so the transition frame contributes to the blend.
            float step = deltaTimeS / _thresholds.BlendDurationS;
            RebuiltBlend = Math.Max(0f, Math.Min(1f,
                RebuiltBlend + (State == AlignmentState.Locked ? step : -step)));
        }

        private static bool IsAccuracyValid(double accuracy)
        {
            return !double.IsNaN(accuracy) && !double.IsInfinity(accuracy) && accuracy >= 0;
        }
    }
}
