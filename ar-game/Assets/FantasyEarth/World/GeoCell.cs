using System;
using System.Collections.Generic;
using System.Globalization;

namespace FantasyEarth.World
{
    /// <summary>Version-1 geographic partition. Longitude columns thin toward the poles.</summary>
    public readonly struct GeoCell : IEquatable<GeoCell>
    {
        public const int LatitudeBands=1<<21;
        private const int EquatorialColumns=1<<22;
        private GeoCell(int row,int column) { Row=row; Column=column; }
        public int Row { get; }
        public int Column { get; }
        public int Columns => ColumnsAt(Row);
        public string Key => "g1/"+Row.ToString(CultureInfo.InvariantCulture)+"/"+Column.ToString(CultureInfo.InvariantCulture);

        public static GeoCell At(GeoPoint point)
        {
            int row=RowAt(point.LatitudeDeg);
            int columns=ColumnsAt(row);
            int column=Math.Min(columns-1,(int)Math.Floor((point.LongitudeDeg+180)/360*columns));
            return new GeoCell(row,column);
        }

        public GeoPoint Point(double eastFraction,double northFraction)
        {
            if (!PlanarPoint.Finite(eastFraction) || !PlanarPoint.Finite(northFraction) ||
                eastFraction<=0 || eastFraction>=1 || northFraction<=0 || northFraction>=1)
                throw new ArgumentOutOfRangeException(nameof(eastFraction));
            return new GeoPoint(-90+(Row+northFraction)*180/LatitudeBands,
                -180+(Column+eastFraction)*360/Columns);
        }

        public static IReadOnlyList<GeoCell> Near(GeoPoint point,double radiusM)
        {
            if (!PlanarPoint.Finite(radiusM) || radiusM<0 || radiusM>128) throw new ArgumentOutOfRangeException(nameof(radiusM));
            // Deliberately conservative query bounds. Exact metric inclusion is tested after projection.
            double angular=(radiusM+32)/6000000*180/Math.PI;
            int first=RowAt(Math.Max(-90,point.LatitudeDeg-angular));
            int last=RowAt(Math.Min(90,point.LatitudeDeg+angular));
            var cells=new List<GeoCell>();
            for (int row=first;row<=last;row++)
            {
                int columns=ColumnsAt(row);
                int centre=Math.Min(columns-1,(int)Math.Floor((point.LongitudeDeg+180)/360*columns));
                double extreme=Math.Min(90,Math.Abs(point.LatitudeDeg)+angular);
                int steps=extreme>=90 ? columns : (int)Math.Ceiling(angular/(360.0/columns*Math.Cos(extreme*Math.PI/180)))+1;
                if (steps*2+1>=columns)
                    for (int col=0;col<columns;col++) cells.Add(new GeoCell(row,col));
                else
                    for (int offset=-steps;offset<=steps;offset++)
                        cells.Add(new GeoCell(row,(centre+offset+columns)%columns));
                if (cells.Count>4096) throw new ArgumentException("Geographic query exceeds its cell budget.",nameof(radiusM));
            }
            cells.Sort((a,b)=> { int row=a.Row.CompareTo(b.Row); return row==0 ? a.Column.CompareTo(b.Column) : row; });
            return cells.AsReadOnly();
        }

        private static int RowAt(double latitude) => Math.Min(LatitudeBands-1,Math.Max(0,(int)Math.Floor((latitude+90)/180*LatitudeBands)));
        private static int ColumnsAt(int row)
        {
            double latitude=-90+(row+.5)*180/LatitudeBands;
            double target=EquatorialColumns*Math.Cos(latitude*Math.PI/180);
            int columns=EquatorialColumns;
            while (columns>1 && columns>target) columns/=2;
            return columns;
        }
        public bool Equals(GeoCell other) => Row==other.Row && Column==other.Column;
        public override bool Equals(object obj) => obj is GeoCell other && Equals(other);
        public override int GetHashCode() { unchecked { return Row*397^Column; } }
    }
}
