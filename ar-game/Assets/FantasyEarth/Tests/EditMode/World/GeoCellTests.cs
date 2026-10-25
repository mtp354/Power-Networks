using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.World
{
    public class GeoCellTests
    {
        [TestCase(0,0)]
        [TestCase(90,0)]
        [TestCase(-90,40)]
        [TestCase(89.9995,179.9999)]
        [TestCase(0,179.99999)]
        public void CellContainsItsInteriorAndQueriesStayBounded(double latitude,double longitude)
        {
            var origin=new GeoPoint(latitude,longitude);
            GeoCell cell=GeoCell.At(origin);
            GeoPoint interior=cell.Point(.5,.5);
            Assert.AreEqual(cell,GeoCell.At(interior));
            var nearby=GeoCell.Near(origin,64);
            Assert.That(nearby.Count,Is.InRange(1,4096));
            Assert.Contains(cell,new System.Collections.Generic.List<GeoCell>(nearby));
            var ids=new System.Collections.Generic.HashSet<GeoCell>(nearby);
            Assert.AreEqual(nearby.Count,ids.Count);
        }

        [Test]
        public void DatelineAliasesShareCellAndNearbyQueryIncludesBothSides()
        {
            Assert.AreEqual(GeoCell.At(new GeoPoint(0,-180)),GeoCell.At(new GeoPoint(0,180)));
            var near=GeoCell.Near(new GeoPoint(0,179.99999),64);
            bool west=false,east=false;
            foreach (var cell in near) { double lon=cell.Point(.5,.5).LongitudeDeg; west|=lon<0; east|=lon>0; }
            Assert.IsTrue(west && east);
        }

        [Test]
        public void VersionOneCellKeysHaveFixedReferenceValues()
        {
            Assert.AreEqual("g1/1048576/1048576",GeoCell.At(new GeoPoint(0,0)).Key);
            Assert.AreEqual("g1/2097151/1",GeoCell.At(new GeoPoint(90,0)).Key);
        }

        [TestCase(0,179.99999)]
        [TestCase(89.9995,40)]
        [TestCase(-90,0)]
        public void GeographicQueryCoversMetricCircleAcrossFrameEdges(double latitude,double longitude)
        {
            var frame=new LocalGeoFrame(new GeoPoint(latitude,longitude));
            var nearby=new System.Collections.Generic.HashSet<GeoCell>(GeoCell.Near(frame.Origin,128));
            for (int degrees=0;degrees<360;degrees+=5)
            {
                double radians=degrees*System.Math.PI/180;
                var point=frame.Unproject(new LocalPoint(128*System.Math.Cos(radians),0,128*System.Math.Sin(radians)));
                Assert.IsTrue(nearby.Contains(GeoCell.At(point.Point)),"Missing circle point at "+degrees);
            }
        }
    }
}
