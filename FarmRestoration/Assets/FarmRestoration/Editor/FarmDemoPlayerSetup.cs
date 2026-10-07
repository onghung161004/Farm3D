using FarmRestoration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    public static class FarmDemoPlayerSetup
    {
        private const string FarmDemoScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";

        [MenuItem("Tools/Farm Restoration/Configure FarmDemo Player and Camera")]
        public static void ConfigurePlayerAndCamera()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != FarmDemoScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Open FarmDemo first",
                    "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command again.",
                    "OK");
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.transform.position = new Vector3(0f, 1f, -6f);
                Undo.RegisterCreatedObjectUndo(player, "Create FarmDemo Player");

                Collider primitiveCollider = player.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    Undo.DestroyObjectImmediate(primitiveCollider);
                }
            }

            Undo.RecordObject(player.transform, "Position FarmDemo Player");
            Vector3 playerPosition = player.transform.position;
            player.transform.position = new Vector3(playerPosition.x, 1f, playerPosition.z);

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<CharacterController>(player);
            }

            Undo.RecordObject(controller, "Configure FarmDemo Character Controller");
            controller.center = Vector3.zero;
            controller.height = 2f;
            controller.radius = 0.45f;

            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement == null)
            {
                movement = Undo.AddComponent<PlayerMovement>(player);
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
                if (!cameraObject.CompareTag("MainCamera"))
                {
                    cameraObject.tag = "MainCamera";
                }

                camera = cameraObject.GetComponent<Camera>() ?? Undo.AddComponent<Camera>(cameraObject);
            }

            if (camera.GetComponent<AudioListener>() == null)
            {
                Undo.AddComponent<AudioListener>(camera.gameObject);
            }

            PlayerFollowCamera followCamera = camera.GetComponent<PlayerFollowCamera>();
            if (followCamera == null)
            {
                followCamera = Undo.AddComponent<PlayerFollowCamera>(camera.gameObject);
            }

            movement.ConfigureCamera(camera);
            followCamera.ConfigureTarget(player.transform);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeGameObject = player;
            Debug.Log("FarmDemo player and follow camera configured. Save the scene with Ctrl+S.");
        }
    }
}
