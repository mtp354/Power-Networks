using System;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.World
{
    public class BuildingFootprintTests
    {
        public static PlanarPoint[] LShape() => new[]
        {
            new PlanarPoint(0, 0), new PlanarPoint(6, 0), new PlanarPoint(6, 2),
            new PlanarPoint(2, 2), new PlanarPoint(2, 6), new PlanarPoint(0, 6)
        };

        [Test]
        public void FootprintCopiesAndNormalizesItsOutline()
        {
            var points = LShape();
            Array.Reverse(points);
            var footprint = new BuildingFootprint("preview:hall", points, 8);
            points[0] = new PlanarPoint(99, 99);
            Assert.AreEqual(20, footprint.AreaM2, 1e-6);
            Assert.AreEqual(6, footprint.WidthM);
            Assert.AreEqual(6, footprint.DepthM);
            Assert.AreEqual(8, footprint.HeightM);
            Assert.Less(footprint.Outline[0].EastM, 99);
        }

        [Test]
        public void ConcaveTriangulationCoversOnlyTheFootprint()
        {
            var footprint = new BuildingFootprint("preview:l", LShape(), 8);
            int[] triangles = FootprintTriangulator.Triangulate(footprint);
            Assert.AreEqual((footprint.Outline.Count - 2) * 3, triangles.Length);
            double area = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = footprint.Outline[triangles[i]];
                var b = footprint.Outline[triangles[i + 1]];
                var c = footprint.Outline[triangles[i + 2]];
                double triangle = ((b.EastM - a.EastM) * (c.NorthM - a.NorthM) -
                    (b.NorthM - a.NorthM) * (c.EastM - a.EastM)) / 2;
                Assert.Greater(triangle, 0);
                area += triangle;
                Assert.IsFalse((a.EastM + b.EastM + c.EastM) / 3 > 2 &&
                    (a.NorthM + b.NorthM + c.NorthM) / 3 > 2, "Triangle bridges the missing corner.");
            }
            Assert.AreEqual(footprint.AreaM2, area, 1e-6);
        }

        [Test]
        public void ClosingVertexWindingAndStartingCornerProduceTheSameCanonicalRing()
        {
            var original = new BuildingFootprint("id", LShape(), 8);
            var shifted = new[] { LShape()[3], LShape()[2], LShape()[1], LShape()[0], LShape()[5], LShape()[4], LShape()[3] };
            var normalized = new BuildingFootprint("id", shifted, 8);
            Assert.AreEqual(original.Outline.Count, normalized.Outline.Count);
            for (int i = 0; i < original.Outline.Count; i++)
            {
                Assert.AreEqual(original.Outline[i].EastM, normalized.Outline[i].EastM);
                Assert.AreEqual(original.Outline[i].NorthM, normalized.Outline[i].NorthM);
            }
        }

        [Test]
        public void InvalidFootprintsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new BuildingFootprint(" ", LShape(), 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BuildingFootprint("id", LShape(), double.NaN));
            Assert.Throws<ArgumentException>(() => new BuildingFootprint("id", new[]
            {
                new PlanarPoint(0, 0), new PlanarPoint(4, 4), new PlanarPoint(0, 4), new PlanarPoint(4, 0)
            }, 8));
        }
    }
}
