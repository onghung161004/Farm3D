using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Widens cloned geometry, not the bridge transform or imported source assets.</summary>
    [DisallowMultipleComponent]
    public sealed class BridgeWidening : MonoBehaviour
    {
        private MeshFilter[] filters;
        private Mesh[] originals;
        private MeshCollider walkway;
        private Mesh originalWalkway;
        private Mesh alignedWalkway, approachSource;
        private MeshFilter approachFilter;
        private GameObject approachObject;
        private Vector3[] railAnchors;
        private readonly Vector3[][] railTiers=new Vector3[3][];
        private readonly Mesh[] approachTiers = new Mesh[2];
        public Mesh CalibratedDeck => alignedWalkway != null ? alignedWalkway : originalWalkway;
        private readonly Mesh[][] tiers = new Mesh[2][];
        private readonly Mesh[] walkwayTiers = new Mesh[2];
        private float centreZ;
        public bool CanUpgrade => isActiveAndEnabled && filters != null && filters.Length > 0
            && walkway != null && walkway.enabled && originalWalkway != null && originalWalkway.isReadable;

        public static BridgeWidening Install(Transform root, MeshCollider walkway, MeshFilter[] filters, Mesh[] originals)
        {
            if (root == null || walkway == null || walkway.sharedMesh == null || !walkway.sharedMesh.isReadable
                || filters == null || originals == null || filters.Length == 0 || filters.Length != originals.Length) return null;
            for (int i = 0; i < filters.Length; i++)
                if (filters[i] == null || originals[i] == null || !originals[i].isReadable) return null;
            BridgeWidening result = root.GetComponent<BridgeWidening>();
            if (result != null) return result;
            result = root.gameObject.AddComponent<BridgeWidening>();
            result.filters = (MeshFilter[])filters.Clone();
            result.originals = (Mesh[])originals.Clone();
            result.walkway = walkway;
            result.originalWalkway = walkway.sharedMesh;
            // The walkway's paired left/right vertices define its fixed longitudinal centreline.
            Vector3[] vertices = result.originalWalkway.vertices;
            if (vertices.Length < 4) { Dispose(result); return null; }
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            foreach (Vector3 vertex in vertices)
            {
                float z = root.InverseTransformPoint(walkway.transform.TransformPoint(vertex)).z;
                min = Mathf.Min(min, z); max = Mathf.Max(max, z);
            }
            result.centreZ = (min + max) * 0.5f;
            if (vertices.Length % 2 == 0)
            {
                int sourceIndex=0;
                for(int i=0;i<filters.Length;i++)if(filters[i].name.EndsWith("LOD0")){sourceIndex=i;break;}
                result.alignedWalkway=BridgeDeckGeometry.Create(root,walkway.transform,result.originalWalkway,
                    filters[sourceIndex],originals[sourceIndex],out result.approachSource,out result.railAnchors);
                if(result.alignedWalkway==null)
                {
                    Debug.LogWarning("Bridge deck calibration failed; upgrade disabled to avoid mismatched collision.",root);
                    Dispose(result);return null;
                }
                result.approachObject=new GameObject("BridgeApproachAlignment",typeof(MeshFilter),typeof(MeshRenderer));
                result.approachObject.transform.SetParent(walkway.transform,false);
                result.approachFilter=result.approachObject.GetComponent<MeshFilter>();
                result.approachObject.GetComponent<MeshRenderer>().sharedMaterial=RestorationProps.WoodFrom(root);
                result.Apply(0);
            }
            return result;
        }

        public bool Apply(int level)
        {
            if (!CanUpgrade || level < 0 || level > 2) return false;
            foreach (MeshFilter filter in filters) if (filter == null) return false;
            if (level > 0 && tiers[level - 1] == null)
            {
                var widened = new Mesh[filters.Length];
                for (int i = 0; i < filters.Length; i++) widened[i] = Widen(originals[i], filters[i].transform, level + 1);
                walkwayTiers[level - 1] = Widen(CalibratedDeck, walkway.transform, level + 1);
                if(approachSource!=null)approachTiers[level-1]=Widen(approachSource,walkway.transform,level+1);
                tiers[level - 1] = widened;
            }
            for (int i = 0; i < filters.Length; i++) filters[i].sharedMesh = level == 0 ? originals[i] : tiers[level - 1][i];
            Mesh deck = level == 0 ? CalibratedDeck : walkwayTiers[level - 1];
            if(approachFilter!=null)approachFilter.sharedMesh=level==0?approachSource:approachTiers[level-1];
            if (walkway.sharedMesh != deck)
            {
                walkway.sharedMesh = null; walkway.sharedMesh = deck;
                Physics.SyncTransforms();
            }
            LODGroup lods = GetComponent<LODGroup>();
            if (lods != null) lods.RecalculateBounds();
            BridgeRailingCollision railing = walkway.GetComponent<BridgeRailingCollision>();
            if (railing != null)
            {
                if(railAnchors!=null)
                {
                    if(railTiers[level]==null)
                    {
                        var points=new Vector3[railAnchors.Length];
                        for(int i=0;i<points.Length;i++)
                        {
                            Vector3 local=transform.InverseTransformPoint(walkway.transform.TransformPoint(railAnchors[i]));
                            local.z=centreZ+(local.z-centreZ)*(level+1);
                            points[i]=walkway.transform.InverseTransformPoint(transform.TransformPoint(local));
                        }
                        railTiers[level]=points;
                    }
                    railing.SetAnchors(railTiers[level],level);
                }
                railing.Refresh();
            }
            return true;
        }

        private Mesh Widen(Mesh source, Transform owner, float factor)
        {
            Mesh copy = Instantiate(source);
            copy.name = source.name + "_Width" + factor;
            Vector3[] vertices = source.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 local = transform.InverseTransformPoint(owner.TransformPoint(vertices[i]));
                local.z = centreZ + (local.z - centreZ) * factor;
                vertices[i] = owner.InverseTransformPoint(transform.TransformPoint(local));
            }
            copy.vertices = vertices;
            copy.RecalculateBounds();
            copy.RecalculateNormals();
            if (copy.uv.Length == copy.vertexCount) copy.RecalculateTangents();
            return copy;
        }

        private static void Dispose(Object item)
        { if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }

        private void OnDestroy()
        {
            if (filters != null) for (int i = 0; i < filters.Length; i++) if (filters[i] != null) filters[i].sharedMesh = originals[i];
            if (walkway != null) walkway.sharedMesh = originalWalkway;
            if(approachObject!=null)Dispose(approachObject);
            if(alignedWalkway!=null)Dispose(alignedWalkway);
            if(approachSource!=null)Dispose(approachSource);
            foreach(Mesh mesh in approachTiers)if(mesh!=null)Dispose(mesh);
            foreach (Mesh[] tier in tiers) if (tier != null) foreach (Mesh mesh in tier) if (mesh != null) Dispose(mesh);
            foreach (Mesh mesh in walkwayTiers) if (mesh != null) Dispose(mesh);
        }
    }
}
