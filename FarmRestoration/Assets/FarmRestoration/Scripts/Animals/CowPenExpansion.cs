using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Extends the existing wood enclosure towards its gate, keeping the rear boundary fixed.</summary>
    [DisallowMultipleComponent]
    public sealed class CowPenExpansion : MonoBehaviour
    {
        public const float Extension = 6f;
        private bool expanded;

        public static void Apply(CowPen pen)
        {
            if (pen == null) return;
            CowPenExpansion expansion = pen.GetComponent<CowPenExpansion>();
            if (expansion == null) expansion = pen.gameObject.AddComponent<CowPenExpansion>();
            pen.SetCapacity(LumberWorkshop.UpgradedCapacity);
            if (expansion.expanded) return;
            expansion.Expand(pen);
        }

        private void Expand(CowPen pen)
        {
            Vector2 original = pen.Footprint;
            float front = -original.y * 0.5f;
            float halfWidth = original.x * 0.5f;
            Vector3 forward = pen.transform.forward;
            var children = new List<Transform>();
            var positions = new List<Vector3>();
            var frontFences = new List<Transform>();
            Transform leftTemplate = null;
            Transform rightTemplate = null;
            foreach (Transform child in pen.transform)
            {
                children.Add(child);
                positions.Add(child.position);
                if (!child.name.StartsWith("CowPenFence_", System.StringComparison.Ordinal)) continue;
                Vector3 local = pen.transform.InverseTransformPoint(child.position);
                bool side = Mathf.Abs(Mathf.Abs(local.x) - halfWidth) < 0.3f;
                if (side)
                {
                    if (local.x < 0f) leftTemplate = child;
                    else rightTemplate = child;
                }
                else if (Mathf.Abs(local.z - front) < 0.3f) frontFences.Add(child);
            }

            // New centre moves forward by half the extension: rear stays in place.
            pen.transform.position -= forward * (Extension * 0.5f);
            pen.Configure(new Vector2(original.x, original.y + Extension));
            for (int i = 0; i < children.Count; i++) children[i].position = positions[i];
            MeshCollider ground = GameObject.Find("WalkableValleyTerrain")?.GetComponent<MeshCollider>();
            foreach (Transform fence in frontFences)
                PlaceOnGround(fence, fence.position - forward * Extension, ground);
            Transform bench = pen.transform.Find("PenUpgradeWorkbench");
            if (bench != null) PlaceOnGround(bench, bench.position - forward * Extension, ground);

            // Reuse existing side panels/materials, not a new prefab or stretched enclosure.
            AddSide(leftTemplate, -halfWidth);
            AddSide(rightTemplate, halfWidth);
            expanded = true;

            void AddSide(Transform template, float x)
            {
                if (template == null) return;
                const int count = 3; // Existing enclosure uses two-metre wood panels.
                for (int i = 0; i < count; i++)
                {
                    Transform panel = Instantiate(template.gameObject, pen.transform).transform;
                    panel.name = "CowPenFence_Expansion_" + (x < 0f ? "Left_" : "Right_") + i;
                    float z = front + Extension * 0.5f - (i + 0.5f) * Extension / count;
                    PlaceOnGround(panel, pen.transform.TransformPoint(new Vector3(x, 0f, z)), ground);
                }
            }
        }

        private static void PlaceOnGround(Transform part, Vector3 position, MeshCollider ground)
        {
            if (ground != null && ground.Raycast(new Ray(position + Vector3.up * 30f, Vector3.down), out RaycastHit hit, 60f))
                position.y = hit.point.y;
            part.position = position;
        }
    }
}
