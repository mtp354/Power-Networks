using System;
using System.Collections.Generic;
using FantasyEarth.Positioning;
using NUnit.Framework;

namespace FantasyEarth.Tests.Positioning
{
    public class AlignmentGateTests
    {
        private static readonly PositioningSample Good = new PositioningSample(true, 1, 1);
        private static readonly PositioningSample Bad = new PositioningSample(true, 3.5, 1);
        private static readonly PositioningSample Between = new PositioningSample(true, 2, 3);

        private static AlignmentGate MakeGate() => new AlignmentGate(AlignmentThresholds.Default);

        private static void Step(AlignmentGate gate, PositioningSample sample, int count)
        {
            for (int i = 0; i < count; i++) gate.Update(sample, 0.1f);
        }

        private static AlignmentGate LockedGate()
        {
            var gate = MakeGate();
            Step(gate, Good, 10);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
            return gate;
        }

        public static IEnumerable<TestCaseData> InvalidAccuracies()
        {
            foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1.0 })
            {
                yield return new TestCaseData(value, 1.0);
                yield return new TestCaseData(1.0, value);
            }
        }

        [Test]
        public void StartsUnlockedWithZeroBlend()
        {
            var gate = MakeGate();
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
            Assert.AreEqual(0f, gate.RebuiltBlend);
        }

        [Test]
        public void LocksOnlyAfterFullEnterDwell()
        {
            var gate = MakeGate();
            Step(gate, Good, 9);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
            Step(gate, Good, 1);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
        }

        [TestCase(1.87, 1.41)]
        [TestCase(1.92, 1.59)]
        [TestCase(2.0, 2.0)]
        public void CalibratedS25AccuracyLocksOnlyAfterFullDwell(double horizontal, double yaw)
        {
            var gate = MakeGate();
            var sample = new PositioningSample(true, horizontal, yaw);
            Step(gate, sample, 9);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
            Step(gate, sample, 1);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
        }

        [TestCase(2.01, 1.41)]
        [TestCase(1.9, 2.01)]
        [TestCase(0.68, 2.59)]
        public void AccuracyOutsideEitherCalibratedLimitDoesNotLock(double horizontal, double yaw)
        {
            var gate = MakeGate();
            Step(gate, new PositioningSample(true, horizontal, yaw), 100);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
            Assert.AreEqual(0f, gate.RebuiltBlend);
        }

        [Test]
        public void NonGoodSampleResetsEnterDwell()
        {
            var gate = MakeGate();
            Step(gate, Good, 5);
            gate.Update(Between, 0.1f);
            Step(gate, Good, 9);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
            Step(gate, Good, 1);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
        }

        [Test]
        public void UnlocksAfterExitDwell()
        {
            var gate = LockedGate();
            Step(gate, Bad, 4);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
            Step(gate, Bad, 1);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
        }

        [Test]
        public void NonBadSampleResetsExitDwell()
        {
            var gate = LockedGate();
            Step(gate, Bad, 4);
            gate.Update(Between, 0.1f);
            Step(gate, Bad, 4);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
            Step(gate, Bad, 1);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
        }

        [Test]
        public void InBetweenValuesKeepCurrentState()
        {
            var locked = LockedGate();
            var unlocked = MakeGate();
            Step(locked, Between, 20);
            Step(unlocked, Between, 20);
            Assert.AreEqual(AlignmentState.Locked, locked.State);
            Assert.AreEqual(AlignmentState.Unlocked, unlocked.State);
        }

        [TestCase(0f)]
        [TestCase(0.1f)]
        public void TrackingLossUnlocksImmediately(float deltaTime)
        {
            var gate = LockedGate();
            gate.Update(new PositioningSample(false, 1, 1), deltaTime);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
        }

        [TestCaseSource(nameof(InvalidAccuracies))]
        public void NaNOrNegativeAccuracyNeverLocks(double horizontal, double yaw)
        {
            var gate = MakeGate();
            Step(gate, new PositioningSample(true, horizontal, yaw), 50);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
            Assert.AreEqual(0f, gate.RebuiltBlend);
        }

        [TestCaseSource(nameof(InvalidAccuracies))]
        public void InvalidAccuracyUnlocksAfterExitDwell(double horizontal, double yaw)
        {
            var gate = LockedGate();
            var sample = new PositioningSample(true, horizontal, yaw);
            Step(gate, sample, 4);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
            Step(gate, sample, 1);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
        }

        [Test]
        public void FlappingTrackingNeverLocks()
        {
            var gate = MakeGate();
            for (int i = 0; i < 100; i++) gate.Update(new PositioningSample(i % 2 == 0, 1, 1), 0.1f);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
        }

        [Test]
        public void BlendRampsOverBlendDuration()
        {
            var gate = LockedGate();
            Assert.AreEqual(0.1f / 0.75f, gate.RebuiltBlend, 1e-4f);
            Step(gate, Good, 7);
            Assert.AreEqual(1f, gate.RebuiltBlend);
            Step(gate, Bad, 4);
            Assert.AreEqual(1f, gate.RebuiltBlend);
            Step(gate, Bad, 1);
            Assert.AreEqual(1f - 0.1f / 0.75f, gate.RebuiltBlend, 1e-4f);
            Step(gate, Bad, 7);
            Assert.AreEqual(0f, gate.RebuiltBlend);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NegativeDeltaTimeIsIgnored(float deltaTime)
        {
            var gate = MakeGate();
            for (int i = 0; i < 20; i++) gate.Update(Good, deltaTime);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
            Assert.AreEqual(0f, gate.RebuiltBlend);

            gate = LockedGate();
            float blend = gate.RebuiltBlend;
            gate.Update(Bad, deltaTime);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
            Assert.AreEqual(blend, gate.RebuiltBlend);
            gate.Update(new PositioningSample(false, 1, 1), deltaTime);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
            Assert.AreEqual(blend, gate.RebuiltBlend);
        }

        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        public void FromEarth_RequiresEnabledAndTracking(bool enabled, bool tracking, bool expected)
        {
            var sample = PositioningSample.FromEarth(enabled, tracking, 1.25, 2.5);
            Assert.AreEqual(expected, sample.IsTracking);
            Assert.AreEqual(1.25, sample.HorizontalAccuracyM);
            Assert.AreEqual(2.5, sample.YawAccuracyDeg);
        }

        [Test]
        public void ThresholdEqualityUsesInclusiveEntryAndExclusiveExit()
        {
            var gate = MakeGate();
            Step(gate, new PositioningSample(true, 2.0, 2.0), 10);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
            Step(gate, new PositioningSample(true, 3.0, 4.0), 20);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
        }

        [Test]
        public void FreshLockNeedsNewDwellAfterTrackingLoss()
        {
            var gate = LockedGate();
            gate.Update(new PositioningSample(false, 1, 1), 0.1f);
            Step(gate, Good, 9);
            Assert.AreEqual(AlignmentState.Unlocked, gate.State);
            Step(gate, Good, 1);
            Assert.AreEqual(AlignmentState.Locked, gate.State);
        }
    }
}
