using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Side-only safety rails sampled from the current paired-row walkway mesh.</summary>
    [DisallowMultipleComponent]
    public sealed class BridgeRailingCollision : MonoBehaviour
    {
        private const float Height = 1.2f;
        private const float Thickness = 0.16f;
        private MeshCollider walkway;
        private Mesh lastMesh;
        private Transform railRoot;
        private BoxCollider[] rails;
        private bool restored;
        private Vector3[] anchors;
        private int anchorLevel=-1;

        public void SetAnchors(Vector3[] points,int level)
        {
            if(anchorLevel==level&&anchors!=null)return;
            anchors=points;anchorLevel=level;lastMesh=null;
        }

        public static BridgeRailingCollision Ensure(MeshCollider deck)
        {
            if (deck == null || deck.sharedMesh == null || !deck.sharedMesh.isReadable
                || deck.sharedMesh.vertexCount < 4 || deck.sharedMesh.vertexCount % 2 != 0) return null;
            BridgeRailingCollision guard = deck.GetComponent<BridgeRailingCollision>();
            if (guard != null) return guard;
            guard = deck.gameObject.AddComponent<BridgeRailingCollision>();
            guard.walkway = deck;
            guard.railRoot = new GameObject("BridgeSideBarriers").transform;
            guard.railRoot.SetParent(deck.transform, false);
            guard.railRoot.gameObject.SetActive(false);
            return guard;
        }

        public void SetRestored(bool value)
        {
            restored = value;
            Refresh();
        }

        public void Refresh()
        {
            if (railRoot == null) return;
            Mesh mesh = walkway != null ? walkway.sharedMesh : null;
            bool valid = restored && walkway != null && walkway.enabled && mesh != null && mesh.isReadable
                && mesh.vertexCount >= 4 && mesh.vertexCount % 2 == 0;
            railRoot.gameObject.SetActive(valid);
            if (!valid || mesh == lastMesh) return;
            bool calibrated=anchors != null && anchors.Length == mesh.vertexCount;
            Vector3[] vertices = calibrated ? anchors : mesh.vertices;
            int count = (vertices.Length / 2 - 1) * 2;
            if (rails != null && rails.Length != count)
            {
                railRoot.gameObject.SetActive(false);
                foreach (BoxCollider rail in rails) if (rail != null) { rail.enabled = false; Dispose(rail.gameObject); }
                rails = null;
            }
            if (rails == null)
            {
                rails = new BoxCollider[count];
                for (int i = 0; i < count; i++)
                {
                    GameObject piece = new GameObject("SideRail" + i, typeof(BoxCollider));
                    piece.transform.SetParent(railRoot, false);
                    piece.layer = walkway.gameObject.layer;
                    rails[i] = piece.GetComponent<BoxCollider>();
                }
            }
            for (int row = 0; row < vertices.Length / 2 - 1; row++)
            for (int side = 0; side < 2; side++)
            {
                Vector3 a = walkway.transform.TransformPoint(vertices[row * 2 + side]);
                Vector3 b = walkway.transform.TransformPoint(vertices[(row + 1) * 2 + side]);
                if(calibrated)
                {
                    Vector3 other=walkway.transform.TransformPoint(vertices[row*2+1-side]);
                    Vector3 outward=Vector3.ProjectOnPlane(a-other,Vector3.up).normalized*Thickness*.5f;
                    a+=outward;b+=outward;
                }
                Vector3 direction = b - a;
                BoxCollider rail = rails[row * 2 + side];
                rail.isTrigger = false;
                rail.transform.SetPositionAndRotation((a + b) * 0.5f + Vector3.up * (Height * 0.5f),
                    direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.identity);
                Vector3 inherited = railRoot.lossyScale;
                rail.transform.localScale = new Vector3(1f / Mathf.Max(0.01f, Mathf.Abs(inherited.x)),
                    1f / Mathf.Max(0.01f, Mathf.Abs(inherited.y)), 1f / Mathf.Max(0.01f, Mathf.Abs(inherited.z)));
                rail.size = new Vector3(Thickness, Height, direction.magnitude + Thickness);
                rail.enabled = direction.sqrMagnitude > 0.0001f;
            }
            lastMesh = mesh;
            railRoot.gameObject.SetActive(true);
            Physics.SyncTransforms();
        }

        private static void Dispose(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        private void OnDestroy() { if (railRoot != null) Dispose(railRoot.gameObject); }
    }
}
