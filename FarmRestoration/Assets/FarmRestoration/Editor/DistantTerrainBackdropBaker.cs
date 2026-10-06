using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Fits the imported terrain to the farm horizon without changing the pack asset.</summary>
    [InitializeOnLoad]
    public static class DistantTerrainBackdropBaker
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string SourcePath = "Assets/Bad_Raccoon/3D Realistic Terrain Free/Terrains/Terrain_A.asset";
        private const string DataPath = "Assets/FarmRestoration/Models/Generated/FarmDistantTerrain.asset";
        private const string LayerPath = "Assets/FarmRestoration/Models/Generated/FarmDistantTerrainLayer.terrainlayer";
        private const string ConnectedDataName = "FarmDistantTerrain_ConnectedV2";
        private static readonly Vector3 FittedSize = new Vector3(700f, 220f, 300f);
        private static readonly Vector3 FittedOrigin = new Vector3(-350f, -35f, 100f);

        static DistantTerrainBackdropBaker()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += FitOpenScene;
        }

        [MenuItem("Tools/Farm Restoration/Fit Distant Terrain Backdrop")]
        public static void FitOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) return;

            Transform root = null;
            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.name != "IllustratedValleyMap") continue;
                root = gameObject.transform;
                break;
            }
            if (root == null) return;

            Transform backdrop = root.Find("DistantRealisticTerrainBackdrop");
            if (backdrop == null) return;

            Terrain terrain = backdrop.GetComponent<Terrain>();
            TerrainData source = AssetDatabase.LoadAssetAtPath<TerrainData>(SourcePath);
            if (terrain == null || source == null)
            {
                Debug.LogWarning("Cannot fit the distant terrain: Terrain component or source asset is missing.", backdrop);
                return;
            }

            TerrainLayer sourceLayer = source.terrainLayers.Length > 0 ? source.terrainLayers[0] : null;
            TerrainLayer fittedLayer = sourceLayer != null ? AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath) : null;
            if (sourceLayer != null && fittedLayer == null)
            {
                fittedLayer = Object.Instantiate(sourceLayer);
                fittedLayer.name = "FarmDistantTerrainLayer";
                AssetDatabase.CreateAsset(fittedLayer, LayerPath);
            }
            if (fittedLayer != null && fittedLayer.tileSize != new Vector2(FittedSize.x, FittedSize.z))
            {
                fittedLayer.tileSize = new Vector2(FittedSize.x, FittedSize.z);
                EditorUtility.SetDirty(fittedLayer);
            }

            TerrainData fitted = AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath);
            if (fitted == null)
            {
                fitted = Object.Instantiate(source);
                fitted.name = "FarmDistantTerrain";
                AssetDatabase.CreateAsset(fitted, DataPath);
            }
            if (fitted.name != ConnectedDataName)
            {
                fitted.size = FittedSize;
                if (fittedLayer != null) fitted.terrainLayers = new[] { fittedLayer };
                DistantTerrainBackdrop.FlattenFrontEdge(fitted, FittedSize, FittedOrigin);
                fitted.name = ConnectedDataName;
                EditorUtility.SetDirty(fitted);
                AssetDatabase.SaveAssets();
            }

            bool changed = false;
            if (terrain.terrainData != fitted)
            {
                terrain.terrainData = fitted;
                changed = true;
            }
            if (!terrain.enabled)
            {
                terrain.enabled = true;
                changed = true;
            }
            if (backdrop.localPosition != FittedOrigin)
            {
                backdrop.localPosition = FittedOrigin;
                changed = true;
            }
            TerrainCollider terrainCollider = backdrop.GetComponent<TerrainCollider>();
            if (terrainCollider != null && terrainCollider.enabled)
            {
                terrainCollider.enabled = false;
                changed = true;
            }
            if (!changed) return;

            EditorUtility.SetDirty(terrain);
            EditorUtility.SetDirty(backdrop);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Distant terrain fitted to the farm horizon. Save FarmDemo with Ctrl+S.", backdrop);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath) EditorApplication.delayCall += FitOpenScene;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += FitOpenScene;
        }

    }
}
