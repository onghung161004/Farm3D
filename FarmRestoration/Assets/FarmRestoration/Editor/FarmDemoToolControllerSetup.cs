using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    public static class FarmDemoToolControllerSetup
    {
        private const string FarmDemoScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";

        [MenuItem("Tools/Farm Restoration/Configure FarmDemo Tools and Interaction")]
        public static void ConfigureToolsAndInteraction()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FarmDemoScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Open FarmDemo first",
                    "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command again.",
                    "OK");
                return;
            }

            GameObject player = FindRootOrChild(scene, "Player");
            if (player == null)
            {
                Debug.LogError("No GameObject named Player exists in the active scene. The scene was left unchanged.");
                return;
            }

            PlayerToolController controller = player.GetComponent<PlayerToolController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<PlayerToolController>(player);
            }
            else
            {
                Undo.RecordObject(controller, "Configure Player Tool Controller");
            }

            Camera camera = FindCamera(scene);
            controller.ConfigureInteractionCamera(camera);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = player;
            Debug.Log("Player tools and interaction configured. Save the scene with Ctrl+S.", controller);
        }

        private static GameObject FindRootOrChild(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < transforms.Length; index++)
                {
                    if (transforms[index].name == objectName)
                    {
                        return transforms[index].gameObject;
                    }
                }
            }

            return null;
        }

        private static Camera FindCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
                if (cameras.Length > 0)
                {
                    return cameras[0];
                }
            }

            return null;
        }
    }
}
