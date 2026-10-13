using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Small decorative wood details, fitted to the existing structure; never add navigation collision.</summary>
    public static class RestorationProps
    {
        public static Bounds LocalBounds(Transform root)
        {
            Bounds result = default;
            bool started = false;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Bounds bounds = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 point = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!started) { result = new Bounds(point, Vector3.zero); started = true; }
                    else result.Encapsulate(point);
                }
            }
            return result;
        }

        public static Transform Group(Transform parent, string name)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        public static GameObject Part(Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, PrimitiveType shape = PrimitiveType.Cube)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Collider>().enabled = false;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        public static void Ground(Transform item, MeshCollider terrain, float heightOffset = 0f)
        {
            if (terrain == null) return;
            Vector3 position = item.position;
            Bounds bounds = terrain.bounds;
            Ray ray = new Ray(new Vector3(position.x, bounds.max.y + 10f, position.z), Vector3.down);
            if (terrain.Raycast(ray, out RaycastHit hit, bounds.size.y + 20f))
                item.position = new Vector3(position.x, hit.point.y + heightOffset, position.z);
        }

        public static Material WoodFrom(Transform model)
        {
            Material fallback = null;
            Material roof = null;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                string lower = material.name.ToLowerInvariant();
                if (lower.Contains("wood") || lower.Contains("stick")) return material;
                if (roof == null && lower.Contains("roof")) roof = material;
                if (fallback == null) fallback = material;
            }
            return roof != null ? roof : fallback;
        }

        public static void Fence(Transform parent, Transform template, Vector3 position, float yaw,
            Material wood, bool broken, MeshCollider ground)
        {
            Transform fence;
            if (template != null)
            {
                fence = Object.Instantiate(template.gameObject, parent).transform;
                Bounds bounds = LocalBounds(template);
                float length = Mathf.Max(bounds.size.x, bounds.size.z);
                fence.localScale = Vector3.one * (length > 0.01f ? 2.2f / length : 1f);
                Vector3 axis = bounds.size.x >= bounds.size.z ? Vector3.right : Vector3.forward;
                fence.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.FromToRotation(axis, Vector3.right);
                foreach (Collider collider in fence.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
            else
            {
                fence = Group(parent, "WoodFence");
                Part(fence, "Rail", Vector3.up * 0.6f, new Vector3(2.2f, 0.12f, 0.10f), wood);
                Part(fence, "PostA", new Vector3(-1f, 0.5f, 0f), new Vector3(0.15f, 1f, 0.15f), wood);
                Part(fence, "PostB", new Vector3(1f, 0.5f, 0f), new Vector3(0.15f, 1f, 0.15f), wood);
                fence.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
            fence.name = broken ? "BrokenWoodFence" : "RepairedWoodFence";
            fence.localPosition = position;
            if (broken) fence.localRotation *= Quaternion.Euler(0f, 0f, 22f);
            Ground(fence, ground);
        }
    }
}
