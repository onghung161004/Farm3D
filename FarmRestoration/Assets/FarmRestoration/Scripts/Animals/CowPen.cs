using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class CowPen : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(10f, 10f);

        public void Configure(Vector2 footprint) => size = footprint;

        public bool Contains(Vector3 worldPosition, float margin = 0f)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return Mathf.Abs(local.x) <= size.x * 0.5f - margin
                && Mathf.Abs(local.z) <= size.y * 0.5f - margin;
        }

        public Vector3 ClampInside(Vector3 worldPosition, float margin)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            local.x = Mathf.Clamp(local.x, -size.x * 0.5f + margin, size.x * 0.5f - margin);
            local.z = Mathf.Clamp(local.z, -size.y * 0.5f + margin, size.y * 0.5f - margin);
            return transform.TransformPoint(local);
        }

        public bool AllowsStep(Vector3 from, Vector3 to)
        {
            if (Contains(from) == Contains(to)) return true;
            Vector3 a = transform.InverseTransformPoint(from);
            Vector3 b = transform.InverseTransformPoint(to);
            float gateZ = -size.y * 0.5f;
            bool crossesGateLine = (a.z <= gateZ && b.z >= gateZ) || (a.z >= gateZ && b.z <= gateZ);
            return crossesGateLine && Mathf.Abs((a.x + b.x) * 0.5f) < 1.35f;
        }
    }
}
