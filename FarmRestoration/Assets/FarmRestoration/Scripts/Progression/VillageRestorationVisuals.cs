using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class VillageRestorationVisuals : MonoBehaviour
    {
        private readonly List<MeshFilter> filters = new List<MeshFilter>();
        private readonly List<Mesh> originalMeshes = new List<Mesh>();
        private readonly List<Mesh> damagedMeshes = new List<Mesh>();
        private Transform broken;
        private Transform repaired;
        private Material plantMaterial;
        private readonly List<HouseTint> houseTints = new List<HouseTint>();

        private sealed class HouseTint
        {
            public Renderer Renderer;
            public int Slot;
            public MaterialPropertyBlock Original;
            public MaterialPropertyBlock Weathered;
        }

        public static VillageRestorationVisuals Install(VillageRepairPoint repair)
        {
            Transform valley = GameObject.Find("IllustratedValleyMap")?.transform;
            if (valley == null || repair == null) return null;
            Transform model = repair.RepairIndex == 0
                ? valley.Find("RiversideVillage/UniformWoodenHamlet/WoodenHouseSouth")
                : valley.Find("PolytopeNatureEnvironment/PolytopeWoodenBridge");
            if (model == null) return null;
            MeshCollider ground = valley.Find("WalkableValleyTerrain")?.GetComponent<MeshCollider>();
            Transform fence = null;
            CowPen pen = Object.FindAnyObjectByType<CowPen>();
            if (pen != null)
                foreach (Transform child in pen.transform)
                    if (child.name.StartsWith("CowPenFence_")) { fence = child; break; }
            VillageRestorationVisuals visual = model.GetComponent<VillageRestorationVisuals>();
            if (visual == null)
            {
                Bounds bounds = RestorationProps.LocalBounds(model);
                if (bounds.size.x < 0.1f || bounds.size.z < 0.1f) return null;
                visual = model.gameObject.AddComponent<VillageRestorationVisuals>();
                if (repair.RepairIndex == 0) visual.BuildHouse(bounds, ground, fence);
                else if (!visual.BuildBridge(ground))
                {
                    if (Application.isPlaying) Destroy(visual);
                    else DestroyImmediate(visual);
                    return null;
                }
            }
            model.gameObject.SetActive(true);
            if (repair.RepairIndex == 0)
            {
                Bounds bounds = visual.houseBounds;
                repair.transform.position = model.TransformPoint(new Vector3(bounds.max.x + 1.8f, bounds.min.y, bounds.center.z));
                RestorationProps.Ground(repair.transform, ground, 0.05f);
            }
            else visual.MoveBridgeSign(repair, ground);
            return visual;
        }

        private Bounds houseBounds;

        private void BuildHouse(Bounds bounds, MeshCollider ground, Transform fence)
        {
            houseBounds = bounds;
            CaptureHouseWeathering();
            broken = RestorationProps.Group(transform, "HouseBeforeRestoration");
            repaired = RestorationProps.Group(transform, "HouseAfterRestoration");
            Material wood = RestorationProps.WoodFrom(transform);
            float x = bounds.max.x + 1.1f, z = bounds.center.z;
            for (int i = 0; i < 2; i++)
                RestorationProps.Fence(broken, fence, new Vector3(x, bounds.min.y, z - 1.4f + i * 2.8f), 90f, wood, true, ground);
            for (int i = 0; i < 4; i++)
                RestorationProps.Fence(repaired, fence, new Vector3(x + 1.2f, bounds.min.y, z - 3.3f + i * 2.2f), 90f, wood, false, ground);
            // Decoration stays at the side of the house, not across its doorway.
            AddDebris(broken, new Vector3(x + 0.4f, bounds.min.y, z), wood, ground, 5);
            Transform bench = RestorationProps.Group(repaired, "RepairedYardBench");
            bench.localPosition = new Vector3(x, bounds.min.y, z + 2f);
            RestorationProps.Part(bench, "Seat", Vector3.up * 0.45f, new Vector3(0.55f, 0.12f, 1.6f), wood);
            RestorationProps.Part(bench, "LegA", new Vector3(0f, 0.2f, -0.55f), new Vector3(0.4f, 0.4f, 0.15f), wood);
            RestorationProps.Part(bench, "LegB", new Vector3(0f, 0.2f, 0.55f), new Vector3(0.4f, 0.4f, 0.15f), wood);
            RestorationProps.Ground(bench, ground);
            Shader shader = wood != null ? wood.shader : Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) plantMaterial = new Material(shader) { color = new Color(0.30f, 0.48f, 0.20f) };
            for (int i = 0; i < 2; i++)
            {
                Transform pot = RestorationProps.Group(repaired, "YardPlant");
                pot.localPosition = new Vector3(x, bounds.min.y, z - 2f - i * 1.1f);
                RestorationProps.Part(pot, "WoodPlanter", Vector3.up * 0.2f, new Vector3(0.55f, 0.4f, 0.55f), wood);
                for (int leaf = 0; leaf < 4; leaf++)
                    RestorationProps.Part(pot, "Leaf", new Vector3(0f, 0.5f, 0f), new Vector3(0.18f, 0.5f, 0.16f), plantMaterial)
                        .transform.localRotation = Quaternion.Euler(25f, leaf * 90f, 20f);
                RestorationProps.Ground(pot, ground);
            }
            ApplyState(false);
        }

        private MeshCollider walkway;
        private BridgeWidening widening;
        private BridgeRailingCollision railing;

        private bool BuildBridge(MeshCollider ground)
        {
            int expected = 0;
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
            {
                string suffix = filter.name.EndsWith("LOD0") ? "LOD0" : filter.name.EndsWith("LOD1") ? "LOD1" : null;
                if (suffix == null) continue;
                expected++;
                Mesh damaged = suffix == null ? null : Resources.Load<Mesh>("BridgeDamaged" + suffix);
                if (damaged == null || filter.sharedMesh == null) continue;
                // A different imported bridge must never receive an incompatible mesh.
                if (filter.sharedMesh.vertexCount != damaged.vertexCount
                    || (filter.sharedMesh.bounds.center - damaged.bounds.center).sqrMagnitude > 0.001f
                    || (filter.sharedMesh.bounds.size - damaged.bounds.size).sqrMagnitude > 0.001f) continue;
                filters.Add(filter); originalMeshes.Add(filter.sharedMesh); damagedMeshes.Add(damaged);
            }
            if (filters.Count == 0 || filters.Count != expected) return false;
            walkway = transform.parent.Find("PolytopeBridgeWalkway")?.GetComponent<MeshCollider>();
            widening = BridgeWidening.Install(transform, walkway, filters.ToArray(), originalMeshes.ToArray());
            railing = BridgeRailingCollision.Ensure(walkway);
            broken = RestorationProps.Group(transform, "BridgeBeforeRestoration");
            repaired = RestorationProps.Group(transform, "BridgeAfterRestoration");
            Material wood = RestorationProps.WoodFrom(transform);
            Vector3 end = BridgeEnd();
            AddDebris(broken, transform.InverseTransformPoint(end + transform.forward * 2.3f), wood, ground, 5);
            ApplyState(false);
            return true;
        }

        private Vector3 BridgeEnd()
        {
            if (walkway != null && walkway.sharedMesh != null && walkway.sharedMesh.isReadable)
            {
                Vector3[] vertices = walkway.sharedMesh.vertices;
                if (vertices.Length >= 2) return walkway.transform.TransformPoint((vertices[0] + vertices[1]) * 0.5f);
            }
            Bounds bounds = originalMeshes.Count == 0 ? default : originalMeshes[0].bounds;
            return filters.Count == 0 ? transform.position : filters[0].transform.TransformPoint(new Vector3(bounds.min.x, bounds.center.y, bounds.center.z));
        }

        private void MoveBridgeSign(VillageRepairPoint repair, MeshCollider ground)
        {
            repair.transform.position = BridgeEnd() - transform.right * 1.8f + transform.forward * 2.5f;
            RestorationProps.Ground(repair.transform, ground, 0.05f);
        }

        private void AddDebris(Transform parent, Vector3 local, Material wood, MeshCollider ground, int count)
        {
            Transform pile = RestorationProps.Group(parent, "BrokenWoodDebris");
            pile.localPosition = local;
            RestorationProps.Ground(pile, ground, 0.08f);
            Vector3 scale = parent.lossyScale;
            pile.localScale = new Vector3(1f / Mathf.Max(0.01f, Mathf.Abs(scale.x)),
                1f / Mathf.Max(0.01f, Mathf.Abs(scale.y)), 1f / Mathf.Max(0.01f, Mathf.Abs(scale.z)));
            for (int i = 0; i < count; i++)
            {
                GameObject plank = RestorationProps.Part(pile, "BrokenPlank", new Vector3((i - 2) * 0.14f, i * 0.06f, (i % 2) * 0.18f), new Vector3(0.75f, 0.09f, 0.12f), wood);
                plank.transform.localRotation = Quaternion.Euler(0f, i * 37f, i * 4f);
            }
        }

        public void ApplyState(bool restored)
        {
            if (broken != null) broken.gameObject.SetActive(!restored);
            if (repaired != null) repaired.gameObject.SetActive(restored);
            if (!restored && widening != null) widening.Apply(0);
            for (int i = 0; i < filters.Count; i++)
                if (filters[i] != null) filters[i].sharedMesh = restored ? originalMeshes[i] : damagedMeshes[i];
            if (restored && widening != null)
                widening.Apply(FarmProgression.Instance != null ? FarmProgression.Instance.BridgeUpgradeLevel : 0);
            if (railing != null) railing.SetRestored(restored);
            foreach (HouseTint tint in houseTints)
                if (tint.Renderer != null) tint.Renderer.SetPropertyBlock(restored ? tint.Original : tint.Weathered, tint.Slot);
        }

        private void CaptureHouseWeathering()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    Material material = materials[slot];
                    if (material == null) continue;
                    string property = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (!material.HasProperty(property)) continue;
                    var original = new MaterialPropertyBlock();
                    var weathered = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original, slot);
                    renderer.GetPropertyBlock(weathered, slot);
                    Color color = original.HasProperty(property) ? original.GetColor(property) : material.GetColor(property);
                    Color faded = Color.Lerp(color, new Color(color.grayscale, color.grayscale, color.grayscale, color.a), 0.55f);
                    faded.r *= 0.62f; faded.g *= 0.62f; faded.b *= 0.62f;
                    weathered.SetColor(property, faded);
                    houseTints.Add(new HouseTint { Renderer = renderer, Slot = slot, Original = original, Weathered = weathered });
                }
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < filters.Count; i++)
                if (filters[i] != null) filters[i].sharedMesh = originalMeshes[i];
            foreach (HouseTint tint in houseTints)
                if (tint.Renderer != null) tint.Renderer.SetPropertyBlock(tint.Original, tint.Slot);
            if (plantMaterial != null)
            {
                if (Application.isPlaying) Destroy(plantMaterial);
                else DestroyImmediate(plantMaterial);
            }
        }
    }
}
