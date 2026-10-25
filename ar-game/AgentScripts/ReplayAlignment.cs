using System;
using System.Collections.Generic;
using System.IO;
using FantasyEarth.Positioning;
using Newtonsoft.Json;

// Run through Unity Pipeline, outside Assets. Input contains only relative time,
// tracking and accuracy measurements; no coordinates or absolute timestamps.
public static class ReplayAlignment
{
    [Serializable] public sealed class Trace { public Sample[] samples; }
    [Serializable] public sealed class Sample
    {
        [JsonProperty(Required = Required.Always)] public double seconds;
        [JsonProperty(Required = Required.Always)] public bool tracking;
        [JsonProperty(Required = Required.Always)] public double horizontalM;
        [JsonProperty(Required = Required.Always)] public double yawDeg;
        public PositioningSample Value => new PositioningSample(tracking, horizontalM, yawDeg);
    }

    [Serializable] public sealed class Result
    {
        public double enterHorizontalM;
        public double enterYawDeg;
        public float enterDwellSeconds;
        public int predictedLockedRecords;
        public int predictedLockTransitions;
        public double? predictedFirstLockSeconds;
        public double? predictedMedianLockedHorizontalM;
        public double? predictedMedianLockedYawDeg;
    }

    [Serializable] public sealed class Report
    {
        public string evidence = "Prediction only: preceding sample held between approximately 2 Hz records; gaps over 1.25 seconds reset tracking. Requires fresh on-device validation.";
        public int samples;
        public Result[] candidates;
    }

    public static string Run()
    {
        string json = File.ReadAllText("Temp/alignment-calibration-input.json");
        if (json.Contains("\"lat\"") || json.Contains("\"lon\"") ||
            json.Contains("\"latitude\"") || json.Contains("\"longitude\""))
            throw new InvalidDataException("Replay input must not contain coordinates.");
        Trace trace = JsonConvert.DeserializeObject<Trace>(json,
            new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
        if (trace?.samples == null || trace.samples.Length < 2)
            throw new InvalidDataException("Replay needs at least two samples.");
        for (int i = 0; i < trace.samples.Length; i++)
        {
            if (trace.samples[i] == null || !Finite(trace.samples[i].seconds) ||
                trace.samples[i].seconds < 0 ||
                (i > 0 && trace.samples[i].seconds < trace.samples[i - 1].seconds))
                throw new InvalidDataException("Replay timestamps must be finite, nonnegative and ordered.");
        }

        var report = new Report { samples = trace.samples.Length, candidates = new Result[4] };
        double[] limits = { 1.5, 1.75, 2.0, 2.25 };
        for (int i = 0; i < limits.Length; i++) report.candidates[i] = Replay(trace.samples, limits[i]);
        string result = JsonConvert.SerializeObject(report, Formatting.Indented);
        File.WriteAllText("Temp/alignment-calibration-results.json", result);
        return result;
    }

    private static Result Replay(Sample[] samples, double horizontalLimit)
    {
        var thresholds = AlignmentThresholds.Default;
        thresholds.EnterHorizontalM = horizontalLimit;
        var gate = new AlignmentGate(thresholds);
        var result = new Result
        {
            enterHorizontalM = horizontalLimit,
            enterYawDeg = thresholds.EnterYawDeg,
            enterDwellSeconds = thresholds.EnterDwellS
        };
        var horizontal = new List<double>();
        var yaw = new List<double>();
        var missing = new PositioningSample(false, double.NaN, double.NaN);
        gate.Update(samples[0].Value, 0);
        for (int i = 1; i < samples.Length; i++)
        {
            Sample previous = samples[i - 1];
            Sample current = samples[i];
            double delta = current.seconds - previous.seconds;
            if (delta > 1.25)
            {
                gate.Update(missing, 0);
            }
            else
            {
                double elapsed = 0;
                while (elapsed < delta - 1e-8)
                {
                    float step = (float)Math.Min(0.1, delta - elapsed);
                    AlignmentState before = gate.State;
                    gate.Update(previous.Value, step);
                    elapsed += step;
                    if (before != AlignmentState.Locked && gate.State == AlignmentState.Locked)
                    {
                        result.predictedLockTransitions++;
                        if (!result.predictedFirstLockSeconds.HasValue)
                            result.predictedFirstLockSeconds = previous.seconds + elapsed;
                    }
                }
            }
            // Reflect immediate tracking loss at the current sample before counting it.
            gate.Update(current.Value, 0);
            if (gate.State != AlignmentState.Locked) continue;
            result.predictedLockedRecords++;
            if (Finite(current.horizontalM) && current.horizontalM >= 0) horizontal.Add(current.horizontalM);
            if (Finite(current.yawDeg) && current.yawDeg >= 0) yaw.Add(current.yawDeg);
        }
        result.predictedMedianLockedHorizontalM = Median(horizontal);
        result.predictedMedianLockedYawDeg = Median(yaw);
        return result;
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static double? Median(List<double> values)
    {
        if (values.Count == 0) return null;
        values.Sort();
        int middle = values.Count / 2;
        return values.Count % 2 == 0 ? (values[middle - 1] + values[middle]) / 2 : values[middle];
    }
}
