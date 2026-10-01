using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    public static class DetailedHeldToolsSetup
    {
        private const string Pack = "Assets/Pandazole_Ultimate_Pack/Pandazole Farm Ranch Pack/Prefabs/";

        [MenuItem("Tools/Farm Restoration/Upgrade Held Tools With Ranch Models")]
        public static void UpgradeHeldTools()
        {
            GameObject player = GameObject.Find("Player");
            Transform holder = player == null ? null : FindToolHolder(player.transform);
            if (holder == null)
            {
                EditorUtility.DisplayDialog("Tool holder missing", "Run Replace Player With Peasant Noland after Add Player Tool Visuals.", "OK");
                return;
            }

            ReplaceTool(holder, "HoeTool", "Prop_SmallFarmingTool_01", 0.62f, new Vector3(0f, -0.20f, 0f), new Vector3(0f, 0f, 85f));
            ReplaceTool(holder, "SeedsTool", "Prop_FoodSack_01", 0.48f, Vector3.zero, new Vector3(0f, 90f, 0f));
            ReplaceTool(holder, "WateringCanTool", "Prop_WateringCan_01", 0.58f, Vector3.zero, new Vector3(0f, 90f, 0f));
            ReplaceTool(holder, "HarvestTool", "Prop_SmallFarmingTool_03", 0.62f, new Vector3(0f, -0.18f, 0f), new Vector3(0f, 0f, 82f));

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null) LiteFarmPackUrpMaterialFix.ConvertMaterialsUnder(holder, lit, "ConvertedHeldTools");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = holder.gameObject;
            Debug.Log("Held tools now use Farm Ranch Pack models.", holder.gameObject);
        }

        private static void ReplaceTool(Transform holder, string visualName, string prefabName, float scale, Vector3 localPosition, Vector3 localEuler)
        {
            Transform root = holder.Find(visualName);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + prefabName + ".prefab");
            if (root == null || prefab == null) return;
            for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            model.name = prefabName;
            model.transform.localPosition = localPosition;
            model.transform.localRotation = Quaternion.Euler(localEuler);
            model.transform.localScale = Vector3.one * scale;
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }

        private static Transform FindToolHolder(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == "ToolHolder") return child;
            return null;
        }
    }
}
