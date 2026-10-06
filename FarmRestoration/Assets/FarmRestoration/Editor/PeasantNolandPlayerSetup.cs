using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    public static class PeasantNolandPlayerSetup
    {
        private const string FarmDemo = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string PeasantPrefab = "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Green(Free Version).prefab";
        private const string PlayerController = "Assets/FarmRestoration/Animations/PeasantPlayer.controller";

        [MenuItem("Tools/Farm Restoration/Replace Player With Peasant Noland")]
        public static void ReplacePlayer()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject player = GameObject.Find("Player");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeasantPrefab);
            if (scene.path != FarmDemo || player == null || prefab == null)
            {
                EditorUtility.DisplayDialog("Peasant setup unavailable", "Open FarmDemo and confirm the Peasant Nolant pack finished importing.", "OK");
                return;
            }

            Transform existingHolder = FindByName(player.transform, "ToolHolder");
            if (existingHolder != null && existingHolder.parent != player.transform)
            {
                existingHolder.SetParent(player.transform, true);
            }
            Transform oldVisual = player.transform.Find("PeasantNolandVisual");
            if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);
            Transform chibi = player.transform.Find("ChibiVisual");
            if (chibi != null) chibi.gameObject.SetActive(false);
            MeshRenderer capsuleRenderer = player.GetComponent<MeshRenderer>();
            if (capsuleRenderer != null) capsuleRenderer.enabled = false;

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
            visual.name = "PeasantNolandVisual";
            visual.transform.localPosition = new Vector3(0f, -1.0f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * 1.5f;

            Animator animator = visual.GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerController);
                if (controller != null) animator.runtimeAnimatorController = controller;
            }
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            PeasantAnimatorDriver driver = player.GetComponent<PeasantAnimatorDriver>();
            if (driver == null) driver = Undo.AddComponent<PeasantAnimatorDriver>(player);
            driver.Configure(movement, animator);

            AttachToolHolder(player, visual.transform);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null) LiteFarmPackUrpMaterialFix.ConvertMaterialsUnder(visual.transform, lit, "ConvertedPeasantNoland");

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("Player now uses Peasant Noland. Movement animation and right-hand tool mounting are configured.", player);
        }

        private static void AttachToolHolder(GameObject player, Transform visual)
        {
            Transform holder = player.transform.Find("ToolHolder");
            if (holder == null) return;
            Transform hand = FindByName(visual, "hand.R");
            if (hand == null)
            {
                Debug.LogWarning("Peasant hand.R bone was not found; the existing ToolHolder was left on Player.", player);
                return;
            }

            holder.SetParent(hand, false);
            // The imported armature uses a 100x bone scale. Cancel it so tool sizes stay human-scale.
            holder.localPosition = new Vector3(0.0002f, 0.0001f, 0.0003f);
            holder.localRotation = Quaternion.Euler(15f, 90f, 78f);
            holder.localScale = Vector3.one * (1f / 150f);
            PlayerToolVisual toolVisual = player.GetComponent<PlayerToolVisual>();
            PlayerToolController controller = player.GetComponent<PlayerToolController>();
            if (toolVisual != null && controller != null) toolVisual.Configure(controller, holder);
        }

        private static Transform FindByName(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
