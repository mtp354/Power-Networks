using System;
using System.IO;
using FantasyEarth.World;
using UnityEngine;
using UnityEngine.Scripting;

namespace FantasyEarth.Data
{
    public static class MapPackReader
    {
        [Serializable,Preserve] private sealed class Pack
        {
            public int mapSchemaVersion;
            public string mapName;
            public double[] coverageBbox,boundsM,defaultCenter;
            public string[] contributors;
            public Surface[] surfaces;
        }
        [Serializable,Preserve] private sealed class Surface
        {
            public string sourceId,sourcesJson;
            public int partIndex,kind,sourceVersion;
            public double[] vertices;
        }
        public static MapRegion Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length>8000000) throw new InvalidDataException("Missing/oversized map pack.");
            try
            {
                var pack=JsonUtility.FromJson<Pack>(json);
                if (pack==null || pack.mapSchemaVersion!=1 || pack.coverageBbox==null || pack.coverageBbox.Length!=4 ||
                    pack.boundsM==null || pack.boundsM.Length!=4 || pack.defaultCenter==null || pack.defaultCenter.Length!=2 ||
                    pack.surfaces==null || pack.surfaces.Length>6000 || pack.contributors==null)
                    throw new InvalidDataException("Unsupported/incomplete map pack.");
                var surfaces=new MapSurface[pack.surfaces.Length];
                for (int i=0;i<surfaces.Length;i++)
                {
                    var item=pack.surfaces[i];
                    if (item==null || item.vertices==null || item.vertices.Length<6 || item.vertices.Length%6!=0 || item.vertices.Length>600000)
                        throw new InvalidDataException("Invalid map surface.");
                    var points=new PlanarPoint[item.vertices.Length/2];
                    for (int p=0;p<points.Length;p++) points[p]=new PlanarPoint(item.vertices[p*2],item.vertices[p*2+1]);
                    surfaces[i]=new MapSurface(item.sourceId,item.partIndex,(MapSurfaceKind)item.kind,points,item.sourceVersion,item.sourcesJson);
                }
                double[] b=pack.boundsM,c=pack.coverageBbox;
                return new MapRegion(pack.mapName,BuildingTileReader.Read(json),surfaces,new MetricBounds(b[0],b[1],b[2],b[3]),
                    c[0],c[1],c[2],c[3],new GeoPoint(pack.defaultCenter[0],pack.defaultCenter[1]),pack.contributors);
            }
            catch (ArgumentException error) { throw new InvalidDataException("Invalid map pack.",error); }
        }
    }
}
