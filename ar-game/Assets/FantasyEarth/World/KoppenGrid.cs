using System;

namespace FantasyEarth.World
{
    /// <summary>
    /// Equirectangular grid of Köppen classes. Row 0 is the band touching 90°N and column 0 starts at 180°W.
    /// </summary>
    public sealed class KoppenGrid
    {
        public const int GlobalWidth = 3600;
        public const int GlobalHeight = 1800;

        private const byte MaxClass = (byte)KoppenClass.EF;

        private readonly byte[] _cells;
        private readonly int _width;
        private readonly int _height;
        private readonly double _cellHeightDeg;
        private readonly double _cellWidthDeg;

        public KoppenGrid(byte[] cells, int width, int height)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Width and height must be positive.");
            }

            if (cells.Length != (long)width * height)
            {
                throw new ArgumentException($"Expected {(long)width * height} cells but got {cells.Length}.", nameof(cells));
            }

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] > MaxClass)
                {
                    throw new ArgumentException($"Cell {i} has value {cells[i]}, which exceeds {MaxClass}.", nameof(cells));
                }
            }

            _cells = cells;
            _width = width;
            _height = height;
            _cellHeightDeg = 180.0 / height;
            _cellWidthDeg = 360.0 / width;
        }

        public KoppenClass ClassAt(GeoPoint p)
        {
            int row = Clamp((int)Math.Floor((90.0 - p.LatitudeDeg) / _cellHeightDeg), _height - 1);
            int col = Clamp((int)Math.Floor((p.LongitudeDeg + 180.0) / _cellWidthDeg), _width - 1);
            return (KoppenClass)_cells[row * _width + col];
        }

        private static int Clamp(int value, int max)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > max ? max : value;
        }
    }
}
