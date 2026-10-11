using FarmRestoration.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Reinstalls the weather/VFX drivers without duplicating scene components.</summary>
    public static class FarmWeatherSceneSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";

        [MenuItem("Tools/Farm Restoration/Install Weather and Action VFX")]
        public static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open FarmDemo.unity before installing weather.", "OK");
                return;
            }

            GameObject lightObject = GameObject.Find("Directional Light");
            GameObject player = GameObject.Find("Player");
            if (lightObject == null || player == null || lightObject.GetComponent<DayNightCycle>() == null
                || player.GetComponent<PlayerToolController>() == null)
            {
                Debug.LogError("Weather setup needs the existing Directional Light/DayNightCycle and Player/PlayerToolController. Scene unchanged.");
                return;
            }

            if (lightObject.GetComponent<FarmWeatherController>() == null)
                Undo.AddComponent<FarmWeatherController>(lightObject);
            FarmWeatherVisuals visuals = lightObject.GetComponent<FarmWeatherVisuals>();
            if (visuals == null) visuals = Undo.AddComponent<FarmWeatherVisuals>(lightObject);
            FarmActionVfx actionVfx = player.GetComponent<FarmActionVfx>();
            if (actionVfx == null) actionVfx = Undo.AddComponent<FarmActionVfx>(player);
            Material particles = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/FarmRestoration/Materials/Generated/FarmParticles.mat");
            if (particles == null) Debug.LogWarning("FarmParticles.mat is missing; VFX will not render.");
            Undo.RecordObject(visuals, "Configure Farm Weather Visuals");
            Undo.RecordObject(actionVfx, "Configure Farm Action VFX");
            visuals.ConfigureMaterial(particles);
            actionVfx.ConfigureMaterial(particles);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Farm weather and action VFX are installed in FarmDemo.");
        }
    }
}
