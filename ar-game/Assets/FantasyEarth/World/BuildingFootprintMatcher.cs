using System;
using System.Collections.Generic;

namespace FantasyEarth.World
{
    public enum BuildingMatchStatus { NoOverlap, LowOverlap, Ambiguous, Matched }

    public readonly struct BuildingMatch
    {
        internal BuildingMatch(BuildingMatchStatus status, CatalogBuilding building, double score, double runnerUp)
        { Status=status; Building=building; IntersectionOverUnion=score; RunnerUpIntersectionOverUnion=runnerUp; }
        public BuildingMatchStatus Status { get; }
        public CatalogBuilding Building { get; }
        public double IntersectionOverUnion { get; }
        public double RunnerUpIntersectionOverUnion { get; }
    }

    /// <summary>Both observed and catalog footprints must already use the catalog's metric frame.</summary>
    public sealed class BuildingFootprintMatcher
    {
        private sealed class Entry { public CatalogBuilding Building; public int[] Triangles; }
        private readonly Entry[] _entries;
        private readonly struct Candidate
        {
            public Candidate(CatalogBuilding building,double score) { Building=building; Score=score; }
            public CatalogBuilding Building { get; }
            public double Score { get; }
        }

        public BuildingFootprintMatcher(BuildingCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            _entries = new Entry[catalog.Buildings.Count];
            for (int i=0;i<_entries.Length;i++) _entries[i]=new Entry
            { Building=catalog.Buildings[i], Triangles=FootprintTriangulator.Triangulate(catalog.Buildings[i].Footprint) };
        }

        public BuildingMatch Match(BuildingFootprint observed, double minimumIoU=.35, double ambiguityMargin=.10)
        {
            if (observed == null) throw new ArgumentNullException(nameof(observed));
            if (!PlanarPoint.Finite(minimumIoU) || minimumIoU <= 0 || minimumIoU > 1 ||
                !PlanarPoint.Finite(ambiguityMargin) || ambiguityMargin < 0 || ambiguityMargin > 1)
                throw new ArgumentOutOfRangeException(nameof(minimumIoU));
            int[] triangles = FootprintTriangulator.Triangulate(observed);
            var identities = new Dictionary<string,Candidate>(StringComparer.Ordinal);
            foreach (var entry in _entries)
            {
                var footprint=entry.Building.Footprint;
                if (!FootprintOverlap.BoundsOverlap(observed,footprint)) continue;
                double area=FootprintOverlap.IntersectionArea(observed,triangles,footprint,entry.Triangles);
                if (area <= 0) continue;
                double score=area/(observed.AreaM2+footprint.AreaM2-area);
                if (!identities.TryGetValue(footprint.StableId,out var previous) || score > previous.Score+1e-10 ||
                    (Math.Abs(score-previous.Score)<1e-10 && entry.Building.PartIndex<previous.Building.PartIndex))
                    identities[footprint.StableId]=new Candidate(entry.Building,score);
            }
            if (identities.Count==0) return new BuildingMatch(BuildingMatchStatus.NoOverlap,null,0,0);
            var ranked = new List<Candidate>(identities.Values);
            ranked.Sort((a,b) =>
            {
                int score=b.Score.CompareTo(a.Score);
                return score!=0 ? score : StringComparer.Ordinal.Compare(a.Building.Footprint.StableId,b.Building.Footprint.StableId);
            });
            Candidate best=ranked[0]; double runner=ranked.Count>1 ? ranked[1].Score : 0;
            var status=best.Score+1e-10<minimumIoU ? BuildingMatchStatus.LowOverlap :
                ranked.Count>1 && best.Score-runner<ambiguityMargin-1e-10 ? BuildingMatchStatus.Ambiguous : BuildingMatchStatus.Matched;
            return new BuildingMatch(status,status==BuildingMatchStatus.Matched ? best.Building : null,best.Score,runner);
        }
    }
}
