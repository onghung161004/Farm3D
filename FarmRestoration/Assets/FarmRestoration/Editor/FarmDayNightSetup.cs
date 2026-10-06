using FarmRestoration.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Installs the runtime day/night driver on FarmDemo's existing directional light.</summary>
    public static class FarmDayNightSetup
    {
        internal static void Install(Scene scene)
        {
            GameObject lightObject = GameObject.Find("Directional Light");
            if (lightObject == null) return;
            Light light = lightObject.GetComponent<Light>();
            if (light == null || light.type != LightType.Directional) return;
            if (lightObject.GetComponent<DayNightCycle>() == null) lightObject.AddComponent<DayNightCycle>();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Tools/Farm Restoration/Install Day Night Cycle")]
        public static void InstallForFarmDemo()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/FarmRestoration/Scenes/FarmDemo.unity")
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command.", "OK");
                return;
            }

            Install(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Day/night cycle installed on Directional Light.");
        }
    }
}
