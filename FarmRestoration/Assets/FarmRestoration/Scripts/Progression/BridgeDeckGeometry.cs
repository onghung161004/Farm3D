using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Calibrates a continuous walking skin to actual plank tops, not decorative rail bounds.</summary>
    public static class BridgeDeckGeometry
    {
        private struct Face
        {
            public Vector3 a,b,c;
            public float MinX => Mathf.Min(a.x,Mathf.Min(b.x,c.x));
            public float MaxX => Mathf.Max(a.x,Mathf.Max(b.x,c.x));
            public Vector3 Centre => (a+b+c)/3f;
            public float Height(float x,float z)
            {
                Vector3 n=Vector3.Cross(b-a,c-a);
                return a.y-(n.x*(x-a.x)+n.z*(z-a.z))/n.y;
            }
            public bool Contains(float x,float z)
            {
                float d=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                if(Mathf.Abs(d)<.000001f)return false;
                float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/d;
                float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/d;
                return u>=-.0001f&&v>=-.0001f&&u+v<=1.0001f;
            }
        }

        public static Mesh Create(Transform root,Transform owner,Mesh oldDeck,MeshFilter filter,Mesh source,
            out Mesh approaches,out Vector3[] railAnchors)
        {
            approaches=null; railAnchors=null;
            if(root==null||owner==null||source==null||!source.isReadable||oldDeck==null||!oldDeck.isReadable)return null;
            Vector3[] vertices=source.vertices;
            Bounds bounds=default;
            for(int i=0;i<vertices.Length;i++)
            {
                vertices[i]=root.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                if(i==0)bounds=new Bounds(vertices[i],Vector3.zero);else bounds.Encapsulate(vertices[i]);
            }
            float centre=bounds.center.z;
            var candidates=new List<Face>(); int[] triangles=source.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var f=new Face{a=vertices[triangles[i]],b=vertices[triangles[i+1]],c=vertices[triangles[i+2]]};
                float minZ=Mathf.Min(f.a.z,Mathf.Min(f.b.z,f.c.z)), maxZ=Mathf.Max(f.a.z,Mathf.Max(f.b.z,f.c.z));
                if(Vector3.Cross(f.b-f.a,f.c-f.a).normalized.y>.45f&&minZ<=centre&&maxZ>=centre)candidates.Add(f);
            }
            var faces=new List<Face>();
            foreach(Face f in candidates)
            {
                float top=float.NegativeInfinity;
                foreach(Face other in candidates)if(other.Contains(f.Centre.x,centre))top=Mathf.Max(top,other.Height(f.Centre.x,centre));
                if(!float.IsNegativeInfinity(top)&&f.Centre.y>=top-.08f)faces.Add(f);
            }
            if(faces.Count<2)return null;
            float minX=float.PositiveInfinity,maxX=float.NegativeInfinity;
            foreach(Face f in faces){minX=Mathf.Min(minX,f.MinX);maxX=Mathf.Max(maxX,f.MaxX);}
            const int InteriorRows=20;
            Vector3[] points=new Vector3[(InteriorRows+2)*2];
            railAnchors=new Vector3[points.Length];
            for(int row=0;row<InteriorRows;row++)
            {
                float x=Mathf.Lerp(minX+.025f,maxX-.025f,row/(float)(InteriorRows-1));
                Span(faces,x,out float left,out float right,out Face nearest);
                for(int side=0;side<2;side++)
                {
                    float z=side==0?left+.003f:right-.003f;
                    float y=nearest.Height(x,z);
                    points[(row+1)*2+side]=new Vector3(x,y+.015f,z);
                    // Find the actual inner face of the elevated rail/post geometry.
                    float railZ=side==0?float.NegativeInfinity:float.PositiveInfinity;
                    foreach(Vector3 v in vertices)
                        if(Mathf.Abs(v.x-x)<.3f&&v.y>nearest.Height(v.x,centre)+.25f&& (side==0?v.z<centre:v.z>centre))
                            railZ=side==0?Mathf.Max(railZ,v.z):Mathf.Min(railZ,v.z);
                    if(float.IsInfinity(railZ))railZ=z;
                    railAnchors[(row+1)*2+side]=new Vector3(x,y,railZ);
                }
            }
            Vector3[] old=oldDeck.vertices;
            if(old.Length<4)return null;
            Vector3 start=root.InverseTransformPoint(owner.TransformPoint((old[0]+old[1])*.5f));
            Vector3 end=root.InverseTransformPoint(owner.TransformPoint((old[old.Length-2]+old[old.Length-1])*.5f));
            if(start.x>end.x){Vector3 swap=start;start=end;end=swap;}
            for(int side=0;side<2;side++)
            {
                points[side]=new Vector3(Mathf.Min(start.x,minX),start.y,points[2+side].z);
                int last=points.Length-2+side,prev=last-2;
                points[last]=new Vector3(Mathf.Max(end.x,maxX),end.y,points[prev].z);
                railAnchors[side]=new Vector3(points[side].x,points[side].y,railAnchors[2+side].z);
                railAnchors[last]=new Vector3(points[last].x,points[last].y,railAnchors[prev].z);
            }
            for(int i=0;i<points.Length;i++)
            {
                points[i]=owner.InverseTransformPoint(root.TransformPoint(points[i]));
                railAnchors[i]=owner.InverseTransformPoint(root.TransformPoint(railAnchors[i]));
            }
            var indices=new int[(points.Length/2-1)*6];
            for(int row=0;row<points.Length/2-1;row++)Quad(indices,row*6,row*2);
            Mesh mesh=Build("AlignedBridgeDeck",points,indices);
            // Only render the short approach joins, leaving the original plank art untouched.
            Vector3[] ends={points[0],points[1],points[2],points[3],points[points.Length-4],points[points.Length-3],points[points.Length-2],points[points.Length-1]};
            int[] joins=new int[12];Quad(joins,0,0);Quad(joins,6,4);
            approaches=Build("BridgeBankJoins",ends,joins);
            return mesh;
        }

        private static void Span(List<Face> faces,float x,out float min,out float max,out Face nearest)
        {
            min=float.PositiveInfinity;max=float.NegativeInfinity; nearest=faces[0];float distance=float.PositiveInfinity;
            foreach(Face f in faces)
            {
                float d=Mathf.Max(0,Mathf.Max(f.MinX-x,x-f.MaxX));
                if(d<distance-.00001f||(Mathf.Abs(d-distance)<.00001f&&f.Height(x,f.Centre.z)>nearest.Height(x,nearest.Centre.z)))
                {distance=d;nearest=f;}
                if(x<f.MinX-.0001f||x>f.MaxX+.0001f)continue;
                Edge(f.a,f.b,x,ref min,ref max);Edge(f.b,f.c,x,ref min,ref max);Edge(f.c,f.a,x,ref min,ref max);
            }
            if(float.IsInfinity(min)||max-min<.1f)
            {min=Mathf.Min(nearest.a.z,Mathf.Min(nearest.b.z,nearest.c.z));max=Mathf.Max(nearest.a.z,Mathf.Max(nearest.b.z,nearest.c.z));}
        }
        private static void Edge(Vector3 a,Vector3 b,float x,ref float min,ref float max)
        {
            if(x<Mathf.Min(a.x,b.x)-.0001f||x>Mathf.Max(a.x,b.x)+.0001f)return;
            float z=Mathf.Abs(b.x-a.x)<.00001f?a.z:Mathf.Lerp(a.z,b.z,(x-a.x)/(b.x-a.x));
            min=Mathf.Min(min,z);max=Mathf.Max(max,z);
        }
        private static void Quad(int[] indices,int t,int a)
        {indices[t]=a;indices[t+1]=a+1;indices[t+2]=a+2;indices[t+3]=a+1;indices[t+4]=a+3;indices[t+5]=a+2;}
        private static Mesh Build(string name,Vector3[] vertices,int[] triangles)
        {
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=triangles;
            var uv=new Vector2[vertices.Length];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(i%2,i/2f);
            mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
