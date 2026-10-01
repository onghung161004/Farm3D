using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace FarmRestoration.Editor
{
    /// <summary>Replaces the flat ground presentation with a playable floating farm island at Y=0.</summary>
    public static class FloatingFarmIslandSetup
    {
        private const string FarmDemo = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string MeshFolder = "Assets/FarmRestoration/Models/Generated";
        private const string MaterialFolder = "Assets/FarmRestoration/Materials/HighDetail";
        private const int Segments = 48;

        [MenuItem("Tools/Farm Restoration/Build Floating Farm Island")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FarmDemo)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command.", "OK");
                return;
            }
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;

            GameObject oldGround = GameObject.Find("FarmGround");
            if (oldGround != null) oldGround.SetActive(false);
            Transform root = GetOrCreateRoot(scene, "FloatingFarmIsland").transform;
            ClearChildren(root);

            CreateSurface(root, lit);
            CreateCliff(root, lit);
            CreateRockLedges(root, lit);
            CreateRoots(root, lit);
            CreateClouds(root, lit);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Floating farm island created. Its walkable grass surface remains at Y=0.", root.gameObject);
        }

        private static void CreateSurface(Transform root, Shader lit)
        {
            GameObject surface = new GameObject("WalkableGrassSurface", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            surface.transform.SetParent(root, false);
            Mesh mesh = GetMesh("FloatingIslandSurface");
            Vector3[] vertices = new Vector3[Segments + 1];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < Segments; i++) vertices[i + 1] = RingPoint(i, 26f, 0f);
            int[] triangles = new int[Segments * 3];
            for (int i = 0; i < Segments; i++)
            {
                int next = (i + 1) % Segments;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = next + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            surface.GetComponent<MeshFilter>().sharedMesh = mesh;
            surface.GetComponent<MeshCollider>().sharedMesh = mesh;
            surface.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial("IslandGrass", lit, new Color(0.20f, 0.52f, 0.16f));
        }

        private static void CreateCliff(Transform root, Shader lit)
        {
            GameObject cliff = new GameObject("LayeredEarthCliff", typeof(MeshFilter), typeof(MeshRenderer));
            cliff.transform.SetParent(root, false);
            Mesh mesh = GetMesh("FloatingIslandCliff");
            // The wide top and tightly tapered bottom are deliberately exaggerated so the
            // island reads as floating even from the normal behind-the-player camera.
            float[] radii = { 26f, 25.6f, 24.4f, 21.5f, 17.5f, 10.5f, 5.5f };
            float[] heights = { -0.03f, -0.55f, -2.4f, -5.5f, -9.5f, -14f, -17.5f };
            Vector3[] vertices = new Vector3[Segments * radii.Length + 1];
            for (int ring = 0; ring < radii.Length; ring++)
                for (int i = 0; i < Segments; i++) vertices[ring * Segments + i] = RingPoint(i, radii[ring], heights[ring]);
            int bottomCenter = vertices.Length - 1;
            vertices[bottomCenter] = new Vector3(0f, -18.4f, 0f);

            List<int>[] bands = new List<int>[4];
            for (int b = 0; b < bands.Length; b++) bands[b] = new List<int>();
            for (int ring = 0; ring < radii.Length - 1; ring++)
            {
                int band = ring == 0 ? 0 : ring < 3 ? 1 : ring < 5 ? 2 : 3;
                for (int i = 0; i < Segments; i++)
                {
                    int next = (i + 1) % Segments;
                    int a = ring * Segments + i, b = ring * Segments + next;
                    int c = (ring + 1) * Segments + i, d = (ring + 1) * Segments + next;
                    // Counter-clockwise from outside: normals face outward, not into the island.
                    bands[band].Add(a); bands[band].Add(b); bands[band].Add(c);
                    bands[band].Add(b); bands[band].Add(d); bands[band].Add(c);
                }
            }
            int[] cap = new int[Segments * 3];
            for (int i = 0; i < Segments; i++)
            {
                int next = (i + 1) % Segments;
                cap[i * 3] = bottomCenter; cap[i * 3 + 1] = (radii.Length - 1) * Segments + i; cap[i * 3 + 2] = (radii.Length - 1) * Segments + next;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.subMeshCount = 5;
            for (int i = 0; i < bands.Length; i++) mesh.SetTriangles(bands[i], i);
            mesh.SetTriangles(cap, 4); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            cliff.GetComponent<MeshFilter>().sharedMesh = mesh;
            cliff.GetComponent<MeshRenderer>().sharedMaterials = new[]
            {
                GetMaterial("IslandTopsoil", lit, new Color(0.22f, 0.12f, 0.04f)),
                GetMaterial("IslandOchreEarth", lit, new Color(0.39f, 0.19f, 0.055f)),
                GetMaterial("IslandWarmRock", lit, new Color(0.25f, 0.18f, 0.12f)),
                GetMaterial("IslandDeepRock", lit, new Color(0.13f, 0.12f, 0.11f)),
                GetMaterial("IslandDeepRock", lit, new Color(0.13f, 0.12f, 0.11f))
            };
        }

        private static void CreateRockLedges(Transform root, Shader lit)
        {
            Material rock = GetMaterial("IslandRock", lit, new Color(0.18f, 0.20f, 0.19f));
            Material grass = GetMaterial("IslandLedgeGrass", lit, new Color(0.26f, 0.62f, 0.17f));
            for (int i = 0; i < Segments; i += 2)
            {
                Vector3 edge = RingPoint(i, 25.0f, -2.0f);
                CreatePart(root, "CliffRock_" + i, PrimitiveType.Sphere, edge, new Vector3(2.1f, 0.85f, 1.35f), rock, new Vector3(i * 3f, i * 11f, 0f));
                if (i % 4 == 0)
                {
                    Vector3 ledge = RingPoint(i, 23.6f, -3.6f);
                    CreatePart(root, "GrassLedge_" + i, PrimitiveType.Cube, ledge, new Vector3(3.8f, 0.26f, 1.4f), grass, new Vector3(0f, i * (360f / Segments), 0f));
                }
            }
        }

        private static void CreateRoots(Transform root, Shader lit)
        {
            Material rootMaterial = GetMaterial("IslandRoots", lit, new Color(0.095f, 0.052f, 0.022f));
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f + 8f;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(radians) * 7.2f, -16.5f - (i % 3) * 0.45f, Mathf.Sin(radians) * 7.2f);
                GameObject rootSpike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rootSpike.name = "ExposedRoot_" + i;
                rootSpike.transform.SetParent(root, false);
                rootSpike.transform.localPosition = position;
                rootSpike.transform.localScale = new Vector3(0.28f + (i % 2) * 0.08f, 2.4f + (i % 3) * 0.45f, 0.28f + (i % 2) * 0.08f);
                rootSpike.transform.localRotation = Quaternion.Euler(18f + (i % 3) * 8f, angle, 18f + (i % 2) * 13f);
                Object.DestroyImmediate(rootSpike.GetComponent<Collider>());
                rootSpike.GetComponent<Renderer>().sharedMaterial = rootMaterial;
            }
        }

        private static void CreateClouds(Transform root, Shader lit)
        {
            Material cloud = GetMaterial("IslandCloud", lit, new Color(0.98f, 0.99f, 1f));
            Vector3[] positions = { new Vector3(-28f, 8f, -10f), new Vector3(29f, 11f, 5f), new Vector3(-10f, 14f, 28f), new Vector3(24f, 7f, -22f) };
            for (int i = 0; i < positions.Length; i++)
            {
                Transform cloudRoot = new GameObject("Cloud_" + i).transform;
                cloudRoot.SetParent(root, false); cloudRoot.localPosition = positions[i];
                for (int puff = 0; puff < 4; puff++) CreatePart(cloudRoot, "Puff_" + puff, PrimitiveType.Sphere, new Vector3((puff - 1.5f) * 0.85f, (puff % 2) * 0.32f, 0f), new Vector3(1.25f, 0.75f, 0.82f), cloud);
            }
        }

        private static Vector3 RingPoint(int index, float radius, float y)
        {
            float angle = index * Mathf.PI * 2f / Segments;
            float irregular = 1f + 0.05f * Mathf.Sin(index * 2.7f) + 0.035f * Mathf.Cos(index * 5.1f);
            return new Vector3(Mathf.Cos(angle) * radius * irregular, y, Mathf.Sin(angle) * radius * irregular);
        }

        private static void CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, Vector3 euler = default)
        {
            GameObject part = GameObject.CreatePrimitive(type); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition; part.transform.localScale = localScale; part.transform.localRotation = Quaternion.Euler(euler);
            Object.DestroyImmediate(part.GetComponent<Collider>()); part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Mesh GetMesh(string name)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmRestoration/Models")) AssetDatabase.CreateFolder("Assets/FarmRestoration", "Models");
            if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Models", "Generated");
            string path = MeshFolder + "/" + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, path); }
            return mesh;
        }

        private static Material GetMaterial(string name, Shader shader, Color color)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmRestoration/Materials")) AssetDatabase.CreateFolder("Assets/FarmRestoration", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "HighDetail");
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            return material;
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            GameObject created = new GameObject(name); SceneManager.MoveGameObjectToScene(created, scene); return created;
        }

        private static void ClearChildren(Transform root) { for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject); }
    }
}
