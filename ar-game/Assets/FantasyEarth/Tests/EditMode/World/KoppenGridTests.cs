using System;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.World
{
    public class KoppenGridTests
    {
        private const int Width = 36;
        private const int Height = 18;

        private static byte[] MakeCells()
        {
            var cells = new byte[Width * Height];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = (byte)(i % 31);
            }
            return cells;
        }

        private static KoppenClass Expected(int row, int col)
        {
            return (KoppenClass)((row * Width + col) % 31);
        }

        private static KoppenGrid MakeGrid()
        {
            return new KoppenGrid(MakeCells(), Width, Height);
        }

        [Test]
        public void ClassAt_TopLeftCell()
        {
            var cells = MakeCells();
            var grid = new KoppenGrid(cells, Width, Height);
            Assert.AreEqual((KoppenClass)cells[0], grid.ClassAt(new GeoPoint(89.9, -179.9)));
        }

        [Test]
        public void ClassAt_NorthPoleUsesFirstRow()
        {
            Assert.AreEqual(Expected(0, 18), MakeGrid().ClassAt(new GeoPoint(90.0, 0.0)));
        }

        [Test]
        public void ClassAt_SouthPoleUsesLastRow()
        {
            var grid = MakeGrid();
            KoppenClass result = KoppenClass.None;
            Assert.DoesNotThrow(() => result = grid.ClassAt(new GeoPoint(-90.0, 0.0)));
            Assert.AreEqual(Expected(17, 18), result);
        }

        [Test]
        public void ClassAt_Antimeridian()
        {
            var grid = MakeGrid();
            var east = grid.ClassAt(new GeoPoint(0.0, 180.0));
            var west = grid.ClassAt(new GeoPoint(0.0, -180.0));
            Assert.AreEqual(west, east);
            Assert.AreEqual(Expected(9, 0), west);
        }

        [Test]
        public void ClassAt_LastColumn()
        {
            Assert.AreEqual(Expected(9, 35), MakeGrid().ClassAt(new GeoPoint(0.0, 179.99)));
        }

        [Test]
        public void Constructor_RejectsWrongLength()
        {
            Assert.Throws<ArgumentException>(() => new KoppenGrid(new byte[Width * Height - 1], Width, Height));
        }

        [Test]
        public void Constructor_RejectsByteAbove30()
        {
            var cells = MakeCells();
            cells[5] = 31;
            Assert.Throws<ArgumentException>(() => new KoppenGrid(cells, Width, Height));
        }
    }
}
