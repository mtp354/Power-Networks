using System;
using System.Collections.Generic;
using FantasyEarth.Fantasy;
using UnityEngine;
using UnityEngine.Rendering;

namespace FantasyEarth.Rendering
{
    /// <summary>One vertex-colored mesh per bounded prop chunk; no child object/material per prop.</summary>
    public static class GroundPropMeshBuilder
    {
        public const int MaximumPropsPerChunk=512;
        private readonly struct Palette
        {
            public readonly Color Bark,Leaf,Stone,Rune;
            private Palette(Color bark,Color leaf,Color stone,Color rune) { Bark=bark; Leaf=leaf; Stone=stone; Rune=rune; }
            public static Palette For(Biome biome)
            {
                switch (biome)
                {
                    case Biome.Desert: return new Palette(C(.48f,.30f,.16f),C(.30f,.56f,.32f),C(.72f,.48f,.29f),new Color(1,.46f,.15f,.8f));
                    case Biome.Snowy: return new Palette(C(.33f,.30f,.29f),C(.24f,.43f,.40f),C(.58f,.67f,.74f),new Color(.34f,.81f,1,.8f));
                    case Biome.Tropical: return new Palette(C(.48f,.31f,.16f),C(.22f,.62f,.32f),C(.37f,.46f,.43f),new Color(.4f,1,.65f,.8f));
                    default: return new Palette(C(.39f,.25f,.14f),C(.35f,.57f,.24f),C(.48f,.50f,.44f),new Color(.48f,.87f,1,.8f));
                }
            }
        }

        public static Mesh Create(IReadOnlyList<GroundProp> props)
        {
            if (props==null) throw new ArgumentNullException(nameof(props));
            if (props.Count>MaximumPropsPerChunk) throw new ArgumentException("Split props into bounded chunks.",nameof(props));
            var surface=new Surface();
            foreach (var prop in props)
            {
                if (string.IsNullOrWhiteSpace(prop.Id) || prop.Scale<=0 || prop.Scale>2 ||
                    Math.Abs(prop.Position.EastM)>5000 || Math.Abs(prop.Position.NorthM)>5000)
                    throw new ArgumentException("Invalid or unrecentered ground prop.",nameof(props));
                surface.Transform=Matrix4x4.TRS(new Vector3((float)prop.Position.EastM,(float)prop.Position.UpM,(float)prop.Position.NorthM),
                    Quaternion.Euler(0,(float)prop.YawDeg,0),Vector3.one*(float)prop.Scale);
                Palette palette=Palette.For(prop.Biome);
                float variation=.88f+.22f*((prop.Seed>>12)&255)/255f;
                if (prop.Kind==GroundPropKind.Vegetation) Vegetation(surface,prop.Biome,palette,variation);
                else if (prop.Kind==GroundPropKind.Boulder)
                {
                    surface.Blob(new Vector3(0,.72f,0),new Vector3(1.05f,.68f,.88f),palette.Stone*variation,prop.Seed);
                    if (prop.Biome==Biome.Snowy)
                        surface.Blob(new Vector3(.05f,1.35f,0),new Vector3(.74f,.20f,.61f),C(.87f,.94f,.96f),prop.Seed);
                }
                else RuneStone(surface,palette,variation);
            }
            return surface.ToMesh();
        }

        private static void Vegetation(Surface s,Biome biome,Palette p,float variation)
        {
            if (biome==Biome.Desert)
            {
                s.Cylinder(Vector3.zero,new Vector3(0,3.5f,0),.32f,.23f,7,p.Leaf*variation);
                s.Cylinder(new Vector3(0,1.5f,0),new Vector3(-.8f,1.5f,0),.20f,.17f,6,p.Leaf);
                s.Cylinder(new Vector3(-.8f,1.5f,0),new Vector3(-.8f,2.8f,0),.17f,.12f,6,p.Leaf);
                s.Cylinder(new Vector3(0,2,0),new Vector3(.85f,2,0),.18f,.15f,6,p.Leaf);
                s.Cylinder(new Vector3(.85f,2,0),new Vector3(.85f,3.1f,0),.15f,.10f,6,p.Leaf);
                return;
            }
            s.Cylinder(Vector3.zero,new Vector3(0,4.4f,0),.19f,.11f,7,p.Bark);
            if (biome==Biome.Snowy)
            {
                s.Cylinder(new Vector3(0,1.2f,0),new Vector3(0,3.8f,0),2,0,8,p.Leaf*variation);
                s.Cylinder(new Vector3(0,2.4f,0),new Vector3(0,4.8f,0),1.5f,0,8,Color.Lerp(p.Leaf,C(.87f,.94f,.96f),.55f));
                s.Cylinder(new Vector3(0,3.5f,0),new Vector3(0,5.6f,0),1.0f,0,8,C(.87f,.94f,.96f));
            }
            else if (biome==Biome.Tropical)
            {
                for (int leaf=0;leaf<7;leaf++)
                {
                    float angle=leaf*Mathf.PI*2/7;
                    Vector3 along=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    Vector3 side=Vector3.Cross(along,Vector3.up)*.35f;
                    Vector3 a=new Vector3(0,4.4f,0),mid=along*1.15f+Vector3.up*4.7f,tip=along*2.3f+Vector3.up*3.9f;
                    s.Leaf(a,mid-side,tip,mid+side,p.Leaf*variation);
                }
                s.Blob(new Vector3(.22f,4.05f,.1f),Vector3.one*.24f,p.Bark,3);
            }
            else
            {
                s.Cylinder(new Vector3(0,2.1f,0),new Vector3(.8f,3.25f,.3f),.13f,.07f,5,p.Bark);
                s.Blob(new Vector3(0,3.9f,0),new Vector3(1.85f,1.5f,1.7f),p.Leaf*variation,11);
                s.Blob(new Vector3(.8f,3.55f,.4f),new Vector3(1.15f,1.1f,1.2f),p.Leaf*.88f,23);
            }
        }

        private static void RuneStone(Surface s,Palette p,float variation)
        {
            s.Blob(new Vector3(0,.21f,0),new Vector3(.76f,.20f,.57f),p.Leaf*.6f,7);
            s.Box(new Vector3(0,1.17f,0),new Vector3(.88f,2.1f,.46f),p.Stone*variation);
            s.Cylinder(new Vector3(0,2.1f,0),new Vector3(0,2.45f,0),.47f,.25f,5,p.Stone);
            s.Cylinder(new Vector3(0,.7f,-.25f),new Vector3(0,1.8f,-.25f),.035f,.035f,4,p.Rune);
            s.Cylinder(new Vector3(-.23f,1.55f,-.25f),new Vector3(.23f,1.22f,-.25f),.03f,.03f,4,p.Rune);
            s.Cylinder(new Vector3(-.20f,.95f,-.25f),new Vector3(.20f,1.20f,-.25f),.03f,.03f,4,p.Rune);
        }

        private static Color C(float r,float g,float b) => new Color(r,g,b,0);

        private sealed class Surface
        {
            public Matrix4x4 Transform;
            private readonly List<Vector3> _vertices=new List<Vector3>();
            private readonly List<Color> _colors=new List<Color>();
            private readonly List<int> _triangles=new List<int>();
            private void Face(Vector3 a,Vector3 b,Vector3 c,Color color)
            {
                if (Vector3.Cross(b-a,c-a).sqrMagnitude<1e-10f) return;
                int start=_vertices.Count;
                _vertices.Add(Transform.MultiplyPoint3x4(a)); _vertices.Add(Transform.MultiplyPoint3x4(b)); _vertices.Add(Transform.MultiplyPoint3x4(c));
                _colors.Add(color); _colors.Add(color); _colors.Add(color);
                _triangles.Add(start); _triangles.Add(start+1); _triangles.Add(start+2);
            }
            private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            { Face(a,b,c,color); Face(a,c,d,color); }
            public void Leaf(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            { Quad(a,b,c,d,color); Quad(d,c,b,a,color*.72f); }

            public void Cylinder(Vector3 a,Vector3 b,float radiusA,float radiusB,int sides,Color color)
            {
                Vector3 up=(b-a).normalized;
                Vector3 right=Vector3.Cross(up,Vector3.forward).normalized;
                if (right.sqrMagnitude<.01f) right=Vector3.right;
                Vector3 forward=Vector3.Cross(right,up).normalized;
                for (int i=0;i<sides;i++)
                {
                    float angle=i*Mathf.PI*2/sides,next=(i+1)*Mathf.PI*2/sides;
                    Vector3 u=right*Mathf.Cos(angle)+forward*Mathf.Sin(angle),v=right*Mathf.Cos(next)+forward*Mathf.Sin(next);
                    Quad(a+u*radiusA,b+u*radiusB,b+v*radiusB,a+v*radiusA,color);
                    Face(b,b+v*radiusB,b+u*radiusB,color);
                    Face(a,a+u*radiusA,a+v*radiusA,color*.82f);
                }
            }

            public void Blob(Vector3 centre,Vector3 radius,Color color,ulong seed)
            {
                Func<int,int,Vector3> point=(row,column)=>
                {
                    float latitude=-Mathf.PI*.5f+row*Mathf.PI/4;
                    float angle=column*Mathf.PI/4;
                    float variation=.92f+.12f*((seed+(ulong)(row*17+column*31))%13)/12;
                    return centre+Vector3.Scale(new Vector3(Mathf.Cos(latitude)*Mathf.Cos(angle),Mathf.Sin(latitude),
                        Mathf.Cos(latitude)*Mathf.Sin(angle)),radius)*variation;
                };
                for (int row=0;row<4;row++) for (int col=0;col<8;col++)
                    Quad(point(row,col),point(row+1,col),point(row+1,(col+1)%8),point(row,(col+1)%8),color);
            }

            public void Box(Vector3 centre,Vector3 size,Color color)
            {
                Vector3 x=Vector3.right*size.x/2,y=Vector3.up*size.y/2,z=Vector3.forward*size.z/2;
                Quad(centre-x-y-z,centre-x+y-z,centre+x+y-z,centre+x-y-z,color);
                Quad(centre+x-y+z,centre+x+y+z,centre-x+y+z,centre-x-y+z,color);
                Quad(centre-x-y+z,centre-x+y+z,centre-x+y-z,centre-x-y-z,color);
                Quad(centre+x-y-z,centre+x+y-z,centre+x+y+z,centre+x-y+z,color);
                Quad(centre-x+y-z,centre-x+y+z,centre+x+y+z,centre+x+y-z,color);
                Quad(centre-x-y+z,centre-x-y-z,centre+x-y-z,centre+x-y+z,color);
            }

            public Mesh ToMesh()
            {
                var mesh=new Mesh { name="Seeded ground prop chunk",indexFormat=_vertices.Count>65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(_vertices); mesh.SetColors(_colors); mesh.SetTriangles(_triangles,0);
                if (_vertices.Count>0) mesh.RecalculateNormals();
                mesh.RecalculateBounds(); return mesh;
            }
        }
    }
}
