using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Horizontal map edge guard and ground-based recovery, independent of input device.</summary>
    public sealed class PlayerMapBoundary
    {
        public const float EdgeInset = 2f;
        public const float FallDepth = 3f;
        private MeshCollider terrain;
        private Vector3 safePosition;
        private bool hasSafePosition;

        public bool HasTerrain => terrain != null && terrain.enabled && terrain.gameObject.activeInHierarchy
            && terrain.sharedMesh != null;

        public void Configure(MeshCollider ground)
        {
            terrain = ground;
            hasSafePosition = false;
        }

        public Vector3 Constrain(Vector3 position, float playerRadius)
        {
            if (!HasTerrain) return position;
            Bounds bounds = terrain.bounds;
            float inset = Mathf.Max(EdgeInset, playerRadius);
            float xInset = Mathf.Min(inset, bounds.extents.x);
            float zInset = Mathf.Min(inset, bounds.extents.z);
            position.x = Mathf.Clamp(position.x, bounds.min.x + xInset, bounds.max.x - xInset);
            position.z = Mathf.Clamp(position.z, bounds.min.z + zInset, bounds.max.z - zInset);
            return position;
        }

        public bool TryRecover(Vector3 position, out Vector3 recovery)
        {
            recovery = position;
            if (!HasTerrain) return false;
            bool supported = TrySurface(position, out Vector3 surface);
            if (supported && position.y >= surface.y - 0.25f && position.y <= surface.y + 1.5f
                && (Constrain(position, 0f) - position).sqrMagnitude < 0.001f)
            {
                // Store a point just above the actual terrain, not a falling or off-map position.
                safePosition = surface + Vector3.up * 0.25f;
                hasSafePosition = true;
            }
            bool fallen = supported ? position.y < surface.y - FallDepth
                : position.y < terrain.bounds.min.y - 10f;
            if (!fallen) return false;
            Vector3 candidate = hasSafePosition ? safePosition : Constrain(position, 0f);
            if (!TrySurface(candidate, out surface))
            {
                candidate = terrain.bounds.center;
                if (!TrySurface(candidate, out surface)) return false;
            }
            recovery = surface + Vector3.up * 0.25f;
            safePosition = recovery;
            hasSafePosition = true;
            return true;
        }

        private bool TrySurface(Vector3 position, out Vector3 surface)
        {
            Bounds bounds = terrain.bounds;
            Ray ray = new Ray(new Vector3(position.x, bounds.max.y + 10f, position.z), Vector3.down);
            if (terrain.Raycast(ray, out RaycastHit hit, bounds.size.y + 20f))
            {
                surface = hit.point;
                return true;
            }
            surface = default;
            return false;
        }
    }
}
