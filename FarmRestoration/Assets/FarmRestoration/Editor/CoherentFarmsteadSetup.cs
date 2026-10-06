using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Replaces the old showroom-like ranch cluster with a walkable farmstead along the valley trail.</summary>
    public static class CoherentFarmsteadSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string RootName = "CoherentFarmstead";
        private const string TrailStyleMarker = "TrailStyleV2";
        private const string PackTrees = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/";
        private const string PackFence = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
        private const string MeshFolder = "Assets/FarmRestoration/Models/Generated/";

        internal static readonly Vector3 PumpkinCenter = new Vector3(-10f, 0.08f, 0f);
        internal static readonly Vector3 CarrotCenter = new Vector3(8f, 0.08f, 3f);
        internal static readonly Vector3 TomatoCenter = new Vector3(8f, 0.08f, 14f);

        [InitializeOnLoadMethod]
        private static void ScheduleUpgrade()
        {
            EditorApplication.delayCall += ApplyWhenReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += ApplyWhenReady;
            };
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += ApplyWhenReady;
        }

        private static void ApplyWhenReady()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath) return;
            Transform existing = FindRoot(scene, "IllustratedValleyMap")?.transform.Find(RootName);
            if (existing != null)
            {
                if (existing.Find(TrailStyleMarker) == null && SyncTrailMaterial(existing))
                    EditorSceneManager.SaveScene(scene);
                return;
            }
            if (ApplyToScene(scene)) EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Farm Restoration/Rebuild Coherent Farmstead")]
        public static void Rebuild()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo", "Exit Play Mode and open FarmDemo.unity first.", "OK");
                return;
            }
            if (ApplyToScene(scene)) EditorSceneManager.SaveScene(scene);
        }

        internal static bool ApplyToScene(Scene scene)
        {
            Transform valley = FindRoot(scene, "IllustratedValleyMap")?.transform;
            Transform gardens = FindRoot(scene, "CropGardens")?.transform;
            if (valley == null || gardens == null || !PolytopeNatureSetup.HasPack()) return false;
            if (gardens.Find("PumpkinGarden") == null || gardens.Find("CarrotGarden") == null
                || gardens.Find("TomatoGarden") == null) return false;
            if (PolytopeNatureSetup.FarmDirtMaterial() == null
                || PolytopeNatureSetup.PathMaterial() == null) return false;

            // These roots are generated decoration, not farming gameplay or user-authored assets.
            RemoveRoot(scene, "AmericanFarmRanch");
            SetInactive(scene, "FarmZone");
            SetInactive(scene, "LiteFarmPackArt");
            SetInactive(scene, "DetailedArt");
            RemoveChild(valley, RootName);

            MoveGarden(gardens.Find("PumpkinGarden"), PumpkinCenter);
            MoveGarden(gardens.Find("CarrotGarden"), CarrotCenter);
            MoveGarden(gardens.Find("TomatoGarden"), TomatoCenter);
            MoveFarmResidence(valley);

            Transform root = new GameObject(RootName).transform;
            root.SetParent(valley, false);
            BuildTrails(root, PolytopeNatureSetup.PathMaterial());
            SyncTrailMaterial(root);
            BuildOrchard(root);
            BuildFences(root);
            PolytopeNatureSetup.ConvertPackMaterials(root);
            PolytopeNatureSetup.RefreshFarmSurfaceMaterials(valley);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Farmstead rebuilt along the valley road. Old ranch cluster removed; 27 interactive plots retained.", root.gameObject);
            return true;
        }

        private static void MoveGarden(Transform garden, Vector3 target)
        {
            FarmPlot[] plots = garden.GetComponentsInChildren<FarmPlot>(true);
            if (plots.Length == 0) return;
            Vector3 center = Vector3.zero;
            foreach (FarmPlot plot in plots) center += plot.transform.position;
            center /= plots.Length;
            garden.position += target - center;
        }

        private static void MoveFarmResidence(Transform valley)
        {
            Transform house = valley.Find("RiversideVillage/UniformWoodenHamlet/WoodenFarmResidence");
            if (house == null) return;
            house.position = new Vector3(-17f, ReferenceLandscapeSetup.GroundHeightAt(-17f, 10f), 10f);
            house.rotation = Quaternion.Euler(0f, 145f, 0f);
        }

        private static void BuildTrails(Transform parent, Material material)
        {
            Transform trails = Child(parent, "FarmTrails");
            Trail(trails, "MainFarmTrail", new[] { new Vector2(0f, -15f), new Vector2(0f, -7f),
                new Vector2(-1f, 3f), new Vector2(-6f, 14f), new Vector2(-20f, 24f) }, 2.6f, material);
            Trail(trails, "PumpkinBranch", new[] { new Vector2(-0.5f, -2f), new Vector2(-6f, -1f),
                new Vector2(-10f, 0f) }, 1.7f, material);
            Trail(trails, "CarrotBranch", new[] { new Vector2(-1f, 3f), new Vector2(4f, 3f),
                new Vector2(8f, 3f) }, 1.7f, material);
            Trail(trails, "TomatoBranch", new[] { new Vector2(-6f, 14f), new Vector2(0f, 14f),
                new Vector2(8f, 14f) }, 1.7f, material);
            Trail(trails, "HouseBranch", new[] { new Vector2(-6f, 14f), new Vector2(-12f, 11f),
                new Vector2(-17f, 10f) }, 1.7f, material);
        }

        private static bool SyncTrailMaterial(Transform farmstead)
        {
            Material material = PolytopeNatureSetup.PathMaterial();
            Transform trails = farmstead.Find("FarmTrails");
            if (material == null || trails == null) return false;
            foreach (Renderer renderer in trails.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
            if (farmstead.Find(TrailStyleMarker) == null)
                Child(farmstead, TrailStyleMarker);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return true;
        }

        private static void Trail(Transform parent, string name, Vector2[] nodes, float width, Material material)
        {
            GameObject path = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            path.transform.SetParent(parent, false);
            string assetPath = MeshFolder + "Coherent" + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Coherent" + name };
                AssetDatabase.CreateAsset(mesh, assetPath);
            }
            int count = (nodes.Length - 1) * 6 + 1;
            Vector3[] vertices = new Vector3[count * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(count - 1) * 6];
            float distance = 0f;
            Vector2 previous = nodes[0];
            for (int i = 0; i < count; i++)
            {
                int segment = Mathf.Min(nodes.Length - 2, i / 6);
                float t = Mathf.Clamp01((i - segment * 6) / 6f);
                Vector2 point = Vector2.Lerp(nodes[segment], nodes[segment + 1], t);
                Vector2 direction = (nodes[segment + 1] - nodes[segment]).normalized;
                Vector2 side = new Vector2(direction.y, -direction.x);
                distance += Vector2.Distance(previous, point);
                previous = point;
                for (int edge = 0; edge < 2; edge++)
                {
                    Vector2 sample = point + side * (edge == 0 ? -width * 0.5f : width * 0.5f);
                    int index = i * 2 + edge;
                    vertices[index] = new Vector3(sample.x,
                        ReferenceLandscapeSetup.GroundHeightAt(sample.x, sample.y) + 0.065f, sample.y);
                    uv[index] = new Vector2(edge, distance / 3f);
                }
                if (i == count - 1) continue;
                int a = i * 2, tri = i * 6;
                triangles[tri] = a; triangles[tri + 1] = a + 2; triangles[tri + 2] = a + 1;
                triangles[tri + 3] = a + 1; triangles[tri + 4] = a + 2; triangles[tri + 5] = a + 3;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            path.GetComponent<MeshFilter>().sharedMesh = mesh;
            path.GetComponent<MeshRenderer>().sharedMaterial = material;
            EditorUtility.SetDirty(mesh);
        }

        private static void BuildOrchard(Transform parent)
        {
            GameObject apple = AssetDatabase.LoadAssetAtPath<GameObject>(PackTrees + "PT_Fruit_Tree_01_apples.prefab");
            GameObject green = AssetDatabase.LoadAssetAtPath<GameObject>(PackTrees + "PT_Fruit_Tree_01_green.prefab");
            Transform orchard = Child(parent, "FarmOrchard");
            Vector2[] positions = { new Vector2(-27f, 2f), new Vector2(-27f, 12f), new Vector2(-24f, 19f),
                new Vector2(19f, -4f), new Vector2(22f, 5f), new Vector2(19f, 17f), new Vector2(4f, 23f) };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject prefab = i % 2 == 0 ? apple : green;
                if (prefab == null) continue;
                Vector2 point = positions[i];
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, orchard);
                tree.name = "FarmOrchardTree_" + i;
                tree.transform.position = new Vector3(point.x, ReferenceLandscapeSetup.GroundHeightAt(point.x, point.y), point.y);
                tree.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
                tree.transform.localScale = Vector3.one * (1.13f + (i % 3) * 0.12f);
                foreach (Collider collider in tree.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
        }

        private static void BuildFences(Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PackFence);
            if (prefab == null) return;
            Transform fences = Child(parent, "FarmWoodFences");
            FenceLine(prefab, fences, new Vector2(-29f, -3f), new Vector2(-29f, 19f), 10);
            FenceLine(prefab, fences, new Vector2(24f, -5f), new Vector2(24f, 19f), 10);
            FenceLine(prefab, fences, new Vector2(-15f, -3f), new Vector2(-15f, 3f), 3);
            FenceLine(prefab, fences, new Vector2(13f, 0f), new Vector2(13f, 6f), 3);
            FenceLine(prefab, fences, new Vector2(5f, 19f), new Vector2(11f, 19f), 3);
        }

        private static void FenceLine(GameObject prefab, Transform parent, Vector2 a, Vector2 b, int count)
        {
            Vector3 direction = new Vector3(b.x - a.x, 0f, b.y - a.y).normalized;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = Vector2.Lerp(a, b, (i + 0.5f) / count);
                GameObject fence = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                fence.name = "FarmFence_" + parent.childCount;
                fence.transform.position = new Vector3(point.x, ReferenceLandscapeSetup.GroundHeightAt(point.x, point.y), point.y);
                Renderer[] renderers = fence.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                for (int j = 1; j < renderers.Length; j++) bounds.Encapsulate(renderers[j].bounds);
                Vector3 axis = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
                float length = Mathf.Max(bounds.size.x, bounds.size.z);
                fence.transform.rotation = Quaternion.FromToRotation(axis, direction);
                if (length > 0.01f) fence.transform.localScale *= Vector2.Distance(a, b) / count / length;
            }
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) Object.DestroyImmediate(root);
        }

        private static void SetInactive(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) root.SetActive(false);
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }
    }
}
