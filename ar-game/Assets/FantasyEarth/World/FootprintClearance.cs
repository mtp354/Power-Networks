using System;

namespace FantasyEarth.World
{
    public static class FootprintClearance
    {
        /// <summary>Zero inside/on the ring; shortest boundary distance outside, including concave notches.</summary>
        public static double DistanceM(BuildingFootprint footprint,PlanarPoint point)
        {
            if (footprint==null) throw new ArgumentNullException(nameof(footprint));
            bool inside=false;
            double best=double.MaxValue;
            for (int i=0;i<footprint.Outline.Count;i++)
            {
                PlanarPoint a=footprint.Outline[i],b=footprint.Outline[(i+1)%footprint.Outline.Count];
                double x=b.EastM-a.EastM,z=b.NorthM-a.NorthM;
                double t=Math.Max(0,Math.Min(1,((point.EastM-a.EastM)*x+(point.NorthM-a.NorthM)*z)/(x*x+z*z)));
                double dx=point.EastM-a.EastM-t*x,dz=point.NorthM-a.NorthM-t*z;
                best=Math.Min(best,dx*dx+dz*dz);
                if ((a.NorthM>point.NorthM)!=(b.NorthM>point.NorthM) &&
                    point.EastM<(b.EastM-a.EastM)*(point.NorthM-a.NorthM)/(b.NorthM-a.NorthM)+a.EastM) inside=!inside;
            }
            return inside ? 0 : Math.Sqrt(best);
        }
    }
}
