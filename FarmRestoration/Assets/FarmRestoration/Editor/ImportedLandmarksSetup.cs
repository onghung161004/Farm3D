using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Places the supplied bridge and mountain GLBs after their mobile-friendly OBJ conversion.</summary>
    public static class ImportedLandmarksSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string RootName = "IllustratedValleyMap";
        private const string ModelFolder = "Assets/FarmRestoration/Models/ImportedLandmarks/";
        private const string TextureFolder = "Assets/FarmRestoration/Textures/ImportedLandmarks/";
        private const string MaterialFolder = "Assets/FarmRestoration/Materials/ImportedLandmarks";
        private const string WalkwayMeshPath = "Assets/FarmRestoration/Models/Generated/ImportedBridgeWalkway.asset";

        [MenuItem("Tools/Farm Restoration/Place Imported Mountains and Bridge")]
        public static void PlaceInCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Exit Play Mode", "Stop the game before placing the imported landmarks, so the scene changes can be saved.", "OK");
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open FarmDemo, then run this command.", "OK");
                return;
            }
            GameObject rootObject = GameObject.Find(RootName);
            if (rootObject == null)
            {
                EditorUtility.DisplayDialog("Valley map missing", "Build the Illustrated 3D Valley Map first.", "OK");
                return;
            }
            bool bridge = PlaceBridge(rootObject.transform);
            bool mountains = PlaceMountains(rootObject.transform);
            if (!bridge && !mountains)
            {
                Debug.LogError("No imported landmark models were found. Check Models/ImportedLandmarks.", rootObject);
                return;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Imported landmarks placed: bridge=" + bridge + ", mountains=" + mountains + ". Save FarmDemo with Ctrl+S.", rootObject);
        }

        internal static bool PlaceBridge(Transform root)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "RiverBridge.obj");
            Material material = GetMaterial("RiverBridge", 0.4f);
            if (!HasMesh(source) || material == null)
            {
                Debug.LogWarning("Imported bridge OBJ or textures are missing; keeping the existing bridge.");
                return false;
            }
            GameObject bridge = (GameObject)PrefabUtility.InstantiatePrefab(source, root);
            bridge.name = "ImportedBridgeCandidate";
            Renderer renderer = bridge.GetComponentInChildren<Renderer>();
            if (renderer == null || renderer.bounds.size.z < 0.01f)
            {
                Debug.LogError("Imported bridge has no usable renderer or length.", bridge);
                Object.DestroyImmediate(bridge);
                return false;
            }
            RemoveNamedChildren(root, "ImportedRiverBridge", "ImportedBridgeWalkway", "BridgePlank_");
            ReferenceLandscapeSetup.GetBridgeAlignment(out Vector3 bridgeCenter, out Vector3 bridgeDirection);
            bridge.name = "ImportedRiverBridge";
            bridge.transform.localScale = Vector3.one * (10.6f / renderer.bounds.size.z);
            bridge.transform.rotation = Quaternion.LookRotation(bridgeDirection, Vector3.up);
            renderer = bridge.GetComponentInChildren<Renderer>();
            Vector3 offset = new Vector3(bridgeCenter.x - renderer.bounds.center.x,
                0.9f - renderer.bounds.max.y,
                bridgeCenter.z - renderer.bounds.center.z);
            bridge.transform.position += offset;
            foreach (Renderer part in bridge.GetComponentsInChildren<Renderer>(true)) part.sharedMaterial = material;
            foreach (Collider part in bridge.GetComponentsInChildren<Collider>(true)) part.enabled = false;
            CreateBridgeWalkway(root, bridgeCenter, bridgeDirection);
            return true;
        }

        internal static bool PlaceMountains(Transform root)
        {
            GameObject a = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "MountainA.obj");
            GameObject b = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "MountainB.obj");
            Material matA = GetMaterial("MountainA", 0.18f);
            Material matB = GetMaterial("MountainB", 0.18f);
            if (!HasMesh(a) || !HasMesh(b) || matA == null || matB == null)
            {
                Debug.LogWarning("Imported mountain OBJ files or textures are missing; keeping the existing mountain range.");
                return false;
            }
            Transform mountainRoot = root.Find("DistantMountainRange");
            if (mountainRoot == null)
            {
                mountainRoot = new GameObject("DistantMountainRange").transform;
                mountainRoot.SetParent(root, false);
            }
            RemoveNamedChildren(mountainRoot, "Mountain_", "ImportedMountain_");
            // These centres are beyond the walkable terrain, so the new range frames the valley.
            Vector3[] positions =
            {
                new Vector3(-125f, -9f, 136f), new Vector3(-82f, -8f, 147f),
                new Vector3(-37f, -7f, 135f), new Vector3(10f, -9f, 149f),
                new Vector3(57f, -8f, 134f), new Vector3(105f, -10f, 146f)
            };
            float[] heights = { 35f, 43f, 38f, 46f, 39f, 34f };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject source = i % 2 == 0 ? a : b;
                Material material = i % 2 == 0 ? matA : matB;
                GameObject mountain = (GameObject)PrefabUtility.InstantiatePrefab(source, mountainRoot);
                mountain.name = "ImportedMountain_" + (i + 1);
                mountain.transform.rotation = Quaternion.Euler(0f, (i * 73) % 360, 0f);
                Renderer renderer = mountain.GetComponentInChildren<Renderer>();
                if (renderer == null || renderer.bounds.size.y < 0.01f)
                {
                    Debug.LogWarning("Mountain model has no usable renderer.", mountain);
                    Object.DestroyImmediate(mountain);
                    continue;
                }
                mountain.transform.localScale = Vector3.one * (heights[i] / renderer.bounds.size.y);
                renderer = mountain.GetComponentInChildren<Renderer>();
                mountain.transform.position += new Vector3(positions[i].x - renderer.bounds.center.x,
                    positions[i].y - renderer.bounds.min.y,
                    positions[i].z - renderer.bounds.center.z);
                foreach (Renderer part in mountain.GetComponentsInChildren<Renderer>(true)) part.sharedMaterial = material;
                foreach (Collider part in mountain.GetComponentsInChildren<Collider>(true)) part.enabled = false;
            }
            return true;
        }

        private static void CreateBridgeWalkway(Transform root, Vector3 bridgeCenter, Vector3 bridgeDirection)
        {
            GameObject walkway = new GameObject("ImportedBridgeWalkway", typeof(MeshCollider));
            walkway.transform.SetParent(root, false);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(WalkwayMeshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "ImportedBridgeWalkway" };
                AssetDatabase.CreateAsset(mesh, WalkwayMeshPath);
            }
            float[] distances = { -5.4f, -3.1f, 3.1f, 5.4f };
            Vector3 right = new Vector3(-bridgeDirection.z, 0f, bridgeDirection.x);
            Vector3[] vertices = new Vector3[distances.Length * 2];
            List<int> triangles = new List<int>();
            for (int row = 0; row < distances.Length; row++)
            for (int side = 0; side < 2; side++)
            {
                Vector3 point = bridgeCenter + bridgeDirection * distances[row]
                    + right * (side == 0 ? -2.05f : 2.05f);
                point.y = row == 0 || row == distances.Length - 1
                    ? ReferenceLandscapeSetup.GroundHeightAt(point.x, point.z) + 0.04f
                    : 0.05f;
                vertices[row * 2 + side] = point;
            }
            for (int row = 0; row < distances.Length - 1; row++)
            {
                int a = row * 2, b = a + 1, c = a + 2, d = a + 3;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            walkway.GetComponent<MeshCollider>().sharedMesh = mesh;
            EditorUtility.SetDirty(mesh);
        }

        private static Material GetMaterial(string name, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;
            string colourPath = TextureFolder + name + "_Color.png";
            string normalPath = TextureFolder + name + "_Normal.png";
            int textureSize = name == "RiverBridge" ? 2048 : 1024;
            ConfigureTexture(colourPath, false, textureSize);
            ConfigureTexture(normalPath, true, textureSize);
            Texture2D colour = AssetDatabase.LoadAssetAtPath<Texture2D>(colourPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (colour == null || normal == null) return null;
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "ImportedLandmarks");
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", colour);
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTexture(string path, bool normal, int maxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            TextureImporterType type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType == type && importer.maxTextureSize == maxSize && importer.wrapMode == TextureWrapMode.Clamp)
                return;
            importer.textureType = type;
            importer.maxTextureSize = maxSize;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        private static void RemoveNamedChildren(Transform parent, params string[] namesOrPrefixes)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                foreach (string name in namesOrPrefixes)
                {
                    if (child.name != name && !child.name.StartsWith(name)) continue;
                    Object.DestroyImmediate(child.gameObject);
                    break;
                }
            }
        }

        private static bool HasMesh(GameObject source)
        {
            MeshFilter filter = source == null ? null : source.GetComponentInChildren<MeshFilter>(true);
            return filter != null && filter.sharedMesh != null && filter.sharedMesh.vertexCount > 0;
        }
    }
}
