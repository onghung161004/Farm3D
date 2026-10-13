using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Builds the small meadow pond without replacing the farm scene or river.</summary>
    public static class FishingPondSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string WaterPath = "Assets/BluBlu Games/Low Poly Forest Mini Pack/Materials/Water.mat";
        private const string BankPath = "Assets/FarmRestoration/Materials/IllustratedValley/ValleyBank.mat";
        private const string MeshFolder = "Assets/FarmRestoration/Models/Generated/";
        private const string RootName = "FishingPond";
        private const string VersionMarker = "PondBasinV1";
        private const int Segments = 48;
        private static Material waterMaterial;

        internal static bool HasWaterMaterial => WaterMaterial != null;

        private static Material WaterMaterial
        {
            get
            {
                if (waterMaterial == null) waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(WaterPath);
                return waterMaterial;
            }
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.delayCall += RefreshOpenScene;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += RefreshOpenScene;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RefreshOpenScene;
            };
        }

        private static void RefreshOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath) return;
            bool wasDirty = scene.isDirty;
            if (!RefreshExisting(scene)) return;
            AssetDatabase.SaveAssets();
            if (!wasDirty) EditorSceneManager.SaveScene(scene);
            else Debug.Log("Fishing pond built in the open scene. Save FarmDemo with Ctrl+S to preserve your unsaved scene edits.");
        }

        [MenuItem("Tools/Farm Restoration/Build Small Fishing Pond")]
        private static void BuildFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                Debug.LogWarning("Exit Play Mode and open FarmDemo before building the fishing pond.");
                return;
            }
            bool wasDirty = scene.isDirty;
            if (Build(scene))
            {
                AssetDatabase.SaveAssets();
                if (!wasDirty) EditorSceneManager.SaveScene(scene);
                else Debug.Log("Fishing pond built. Save FarmDemo with Ctrl+S after reviewing your other unsaved scene edits.");
            }
        }

        internal static bool RefreshExisting(Scene scene)
        {
            Transform valley = GameObject.Find("IllustratedValleyMap")?.transform;
            if (valley == null || !HasWaterMaterial) return false;
            Transform pond = valley.Find(RootName);
            MeshFilter water = pond?.Find("Water")?.GetComponent<MeshFilter>();
            MeshFilter shore = pond?.Find("Shore")?.GetComponent<MeshFilter>();
            bool valid = pond?.Find(VersionMarker) != null && water != null
                && water.sharedMesh != null && water.sharedMesh.vertexCount == Segments + 1
                && shore != null && shore.sharedMesh != null && shore.sharedMesh.vertexCount == Segments * 2
                && pond.Find("Water").GetComponent<Renderer>()?.sharedMaterial == WaterMaterial
                && pond.Find("Shore").GetComponent<Renderer>()?.sharedMaterial
                    == AssetDatabase.LoadAssetAtPath<Material>(BankPath)
                && IsBasinCarved(valley.Find("WalkableValleyTerrain")?.GetComponent<MeshCollider>());
            return valid ? false : Build(scene);
        }

        internal static bool Build(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath) return false;
            Material water = WaterMaterial;
            Material bank = AssetDatabase.LoadAssetAtPath<Material>(BankPath);
            Transform valley = GameObject.Find("IllustratedValleyMap")?.transform;
            MeshCollider terrain = valley?.Find("WalkableValleyTerrain")?.GetComponent<MeshCollider>();
            if (water == null || water.shader == null || bank == null || terrain?.sharedMesh == null)
            {
                Debug.LogWarning("Fishing pond needs the BluBlu water material, ValleyBank material and walkable valley mesh. No pond was created.");
                return false;
            }
            if (!SculptLocalBasin(terrain)) return false;
            Physics.SyncTransforms();

            Transform pond = valley.Find(RootName);
            if (pond == null)
            {
                pond = new GameObject(RootName).transform;
                pond.SetParent(valley, false);
            }
            BuildWater(pond, water);
            BuildBank(pond, terrain, bank);
            if (pond.Find(VersionMarker) == null) new GameObject(VersionMarker).transform.SetParent(pond, false);
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static bool IsBasinCarved(MeshCollider terrain)
        {
            if (terrain == null || terrain.sharedMesh == null) return false;
            Vector2 center = FishingPondTerrain.Center;
            Ray ray = new Ray(new Vector3(center.x, 100f, center.y), Vector3.down);
            return terrain.Raycast(ray, out RaycastHit hit, 200f)
                && hit.point.y < FishingPondTerrain.WaterLevel - 0.5f;
        }

        private static bool SculptLocalBasin(MeshCollider terrain)
        {
            if (IsBasinCarved(terrain)) return true;
            Mesh mesh = terrain.sharedMesh;
            if (mesh == null) return false;
            Vector3[] vertices = mesh.vertices;
            Undo.RecordObject(mesh, "Sculpt fishing pond basin");
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                if (!FishingPondTerrain.IsNear(new Vector2(vertex.x, vertex.z))) continue;
                vertices[i].y = FishingPondTerrain.Height(vertex.x, vertex.z, vertex.y);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            terrain.sharedMesh = null;
            terrain.sharedMesh = mesh;
            EditorUtility.SetDirty(mesh);
            return true;
        }

        private static void BuildWater(Transform pond, Material material)
        {
            GameObject water = ChildWithMesh(pond, "Water");
            Mesh mesh = MeshAsset("FishingPondWater");
            Vector3[] vertices = new Vector3[Segments + 1];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[Segments * 3];
            Vector2 center = FishingPondTerrain.Center;
            vertices[0] = new Vector3(center.x, FishingPondTerrain.WaterLevel, center.y);
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                float x = Mathf.Cos(angle), z = Mathf.Sin(angle);
                vertices[i + 1] = new Vector3(center.x + x * FishingPondTerrain.WaterRadiusX,
                    FishingPondTerrain.WaterLevel, center.y + z * FishingPondTerrain.WaterRadiusZ);
                uv[i + 1] = new Vector2(0.5f + x * 0.5f, 0.5f + z * 0.5f);
                int t = i * 3;
                triangles[t] = 0;
                triangles[t + 1] = (i + 1) % Segments + 1;
                triangles[t + 2] = i + 1;
            }
            SetMesh(mesh, vertices, uv, triangles);
            water.GetComponent<MeshFilter>().sharedMesh = mesh;
            water.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void BuildBank(Transform pond, MeshCollider terrain, Material material)
        {
            GameObject shore = ChildWithMesh(pond, "Shore");
            Mesh mesh = MeshAsset("FishingPondShore");
            Vector3[] vertices = new Vector3[Segments * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[Segments * 6];
            Vector2 center = FishingPondTerrain.Center;
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                float x = Mathf.Cos(angle), z = Mathf.Sin(angle);
                Vector3 outer = new Vector3(center.x + x * FishingPondTerrain.OuterRadiusX,
                    0f, center.y + z * FishingPondTerrain.OuterRadiusZ);
                Ray ray = new Ray(new Vector3(outer.x, 100f, outer.z), Vector3.down);
                outer.y = terrain.Raycast(ray, out RaycastHit hit, 200f) ? hit.point.y + 0.035f
                    : ReferenceLandscapeSetup.GroundHeightAt(outer.x, outer.z) + 0.035f;
                Vector3 inner = new Vector3(center.x + x * (FishingPondTerrain.WaterRadiusX - 0.08f),
                    FishingPondTerrain.WaterLevel + 0.02f,
                    center.y + z * (FishingPondTerrain.WaterRadiusZ - 0.08f));
                vertices[i * 2] = outer;
                vertices[i * 2 + 1] = inner;
                uv[i * 2] = new Vector2(0f, i / 6f);
                uv[i * 2 + 1] = new Vector2(1f, i / 6f);
                int next = (i + 1) % Segments;
                int t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = i * 2 + 1;
                triangles[t + 2] = next * 2;
                triangles[t + 3] = i * 2 + 1;
                triangles[t + 4] = next * 2 + 1;
                triangles[t + 5] = next * 2;
            }
            SetMesh(mesh, vertices, uv, triangles);
            shore.GetComponent<MeshFilter>().sharedMesh = mesh;
            shore.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static GameObject ChildWithMesh(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                gameObject.transform.SetParent(parent, false);
                return gameObject;
            }
            if (child.GetComponent<MeshFilter>() == null) child.gameObject.AddComponent<MeshFilter>();
            if (child.GetComponent<MeshRenderer>() == null) child.gameObject.AddComponent<MeshRenderer>();
            return child.gameObject;
        }

        private static Mesh MeshAsset(string name)
        {
            string path = MeshFolder + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void SetMesh(Mesh mesh, Vector3[] vertices, Vector2[] uv, int[] triangles)
        {
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
        }
    }
}
