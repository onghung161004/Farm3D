using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Installs the dairy pen and the imported cow pack in FarmDemo without rebuilding the user's map.</summary>
    public static class DairyHerdSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string RootName = "DairyHerd";
        private const string CowPrefabPath = "Assets/Shepherd_Valley/HDRP/Prefabs/P_Cow.prefab";
        private const string PackRoot = "Assets/Shepherd_Valley/HDRP/";
        private const string OutputFolder = "Assets/FarmRestoration/Materials/Generated/";
        private const string FencePath = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
        private const string RanchPath = "Assets/Pandazole_Ultimate_Pack/Pandazole Farm Ranch Pack/Prefabs/";
        private const float CowHeight = 3.2f;
        private static readonly Vector2[] CowSpawns =
        {
            new Vector2(-12f, -20f), new Vector2(6f, -24f), new Vector2(24f, -13f),
            new Vector2(29f, 23f), new Vector2(5f, 31f)
        };

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.delayCall += AutoInstall;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += AutoInstall;
        }

        private static void AutoInstall()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath) return;
            bool wasDirty = scene.isDirty;
            bool changed = HasHerd(scene) ? UpgradeExistingHerd(scene) : Install(scene);
            if (!changed) return;
            if (!wasDirty) EditorSceneManager.SaveScene(scene);
            else Debug.Log("Dairy herd updated in the open scene. Save FarmDemo with Ctrl+S to keep your other unsaved scene edits.");
        }

        [MenuItem("Tools/Farm Restoration/Install Dairy Herd")]
        public static void InstallFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo", "Exit Play Mode and open FarmDemo.unity first.", "OK");
                return;
            }
            if (HasHerd(scene))
            {
                if (UpgradeExistingHerd(scene)) EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = GameObject.Find(RootName);
                return;
            }
            if (Install(scene)) EditorSceneManager.SaveScene(scene);
        }

        private static bool HasHerd(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == RootName) return true;
            return false;
        }

        private static bool Install(Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CowPrefabPath);
            GameObject fence = AssetDatabase.LoadAssetAtPath<GameObject>(FencePath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            GameObject terrain = GameObject.Find("WalkableValleyTerrain");
            MeshCollider ground = terrain != null ? terrain.GetComponent<MeshCollider>() : null;
            if (prefab == null || fence == null || shader == null || ground == null)
            {
                Debug.LogWarning("Dairy herd needs the cow pack, Polytope fence, URP Lit and the walkable valley terrain. No scene changes were made.");
                return false;
            }

            Material cowMaterial = EnsureCowMaterial(shader);
            Material ropeMaterial = EnsureRopeMaterial(shader);
            AnimatorController controller = EnsureCowController();
            if (cowMaterial == null || ropeMaterial == null || controller == null) return false;

            Transform root = new GameObject(RootName).transform;
            SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            CowPen pen = CreatePen(root, fence);
            PlaceHay(root);
            for (int i = 0; i < CowSpawns.Length; i++)
                CreateCow(prefab, root, pen, ground, cowMaterial, ropeMaterial, controller, i);
            PolytopeNatureSetup.ConvertPackMaterials(root);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Dairy herd installed: five cows, fenced pasture, feed hay and feeding/milking interactions.", root.gameObject);
            return true;
        }

        private static bool UpgradeExistingHerd(Scene scene)
        {
            Transform herd = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == RootName) { herd = root.transform; break; }
            if (herd == null) return false;

            bool changed = false;
            Transform shelter = herd.Find("DairyShelter");
            if (shelter != null)
            {
                // Only remove the shelter generated by this installer, never another village house.
                Undo.DestroyObjectImmediate(shelter.gameObject);
                changed = true;
            }
            foreach (CowAnimal cow in herd.GetComponentsInChildren<CowAnimal>(true))
            {
                Transform visual = cow.transform.Find("CowModel");
                if (visual == null) continue;
                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                Bounds bounds = CombinedBounds(renderers);
                if (bounds.size.y > 0.01f && Mathf.Abs(bounds.size.y - CowHeight) > 0.03f)
                {
                    Undo.RecordObject(visual, "Resize dairy cow");
                    visual.localScale *= CowHeight / bounds.size.y;
                    bounds = CombinedBounds(renderers);
                    visual.position += Vector3.up * (cow.transform.position.y - bounds.min.y);
                    changed = true;
                }
                CapsuleCollider collider = cow.GetComponent<CapsuleCollider>();
                float scale = Mathf.Max(0.01f, cow.transform.lossyScale.y);
                if (collider != null && Mathf.Abs(collider.height * scale - CowHeight) > 0.03f)
                {
                    Undo.RecordObject(collider, "Resize dairy cow collider");
                    collider.radius = 1.15f / scale;
                    collider.height = CowHeight / scale;
                    collider.center = Vector3.up * collider.height * 0.5f;
                    changed = true;
                }
            }
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
            return changed;
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Material EnsureCowMaterial(Shader shader)
        {
            string path = OutputFolder + "CowURP.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "CowURP" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PackRoot + "Textures/T_Cow_B.png"));
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(PackRoot + "Textures/T_Cow_N.png");
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureRopeMaterial(Shader shader)
        {
            string path = OutputFolder + "CowLeadRope.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "CowLeadRope" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.48f, 0.28f, 0.11f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimatorController EnsureCowController()
        {
            string path = OutputFolder + "CowFarm.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null) return controller;
            AnimationClip idle = LoadClip("A_Cow_Idle_01");
            AnimationClip walk = LoadClip("A_Cow_Walk_01");
            AnimationClip eating = LoadClip("A_Cow_Eating_01");
            if (idle == null || walk == null || eating == null)
            {
                Debug.LogError("Cow animation clips could not be imported. Check Shepherd_Valley/HDRP/Animations/Cow.");
                return null;
            }
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle"); idleState.motion = idle;
            AnimatorState walkState = machine.AddState("Walk"); walkState.motion = walk;
            AnimatorState eatingState = machine.AddState("Eating"); eatingState.motion = eating;
            machine.defaultState = idleState;
            return controller;
        }

        private static AnimationClip LoadClip(string name)
        {
            string path = PackRoot + "Animations/Cow/" + name + ".fbx";
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
            return null;
        }

        private static CowPen CreatePen(Transform root, GameObject fence)
        {
            Vector3 center = OnGround(-24f, 12f);
            GameObject penObject = new GameObject("DairyPen");
            penObject.transform.SetParent(root);
            penObject.transform.position = center;
            CowPen pen = penObject.AddComponent<CowPen>();
            pen.Configure(new Vector2(8f, 12f));
            FenceSide(fence, penObject.transform, new Vector2(-28f, 18f), new Vector2(-20f, 18f), 4);
            FenceSide(fence, penObject.transform, new Vector2(-28f, 6f), new Vector2(-25.5f, 6f), 1);
            FenceSide(fence, penObject.transform, new Vector2(-22.5f, 6f), new Vector2(-20f, 6f), 1);
            FenceSide(fence, penObject.transform, new Vector2(-28f, 6f), new Vector2(-28f, 18f), 6);
            FenceSide(fence, penObject.transform, new Vector2(-20f, 6f), new Vector2(-20f, 18f), 6);
            return pen;
        }

        private static void FenceSide(GameObject prefab, Transform parent, Vector2 a, Vector2 b, int count)
        {
            Vector3 desiredDirection = new Vector3(b.x - a.x, 0f, b.y - a.y).normalized;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, (i + 0.5f) / count);
                GameObject part = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                part.name = "CowPenFence_" + parent.childCount;
                part.transform.position = OnGround(p.x, p.y);
                Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                for (int j = 1; j < renderers.Length; j++) bounds.Encapsulate(renderers[j].bounds);
                Vector3 axis = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
                float length = Mathf.Max(bounds.size.x, bounds.size.z);
                part.transform.rotation = Quaternion.FromToRotation(axis, desiredDirection);
                if (length > 0.01f) part.transform.localScale *= Vector2.Distance(a, b) / count / length;
            }
        }

        private static void PlaceHay(Transform root)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RanchPath + "Prop_Haystack_01.prefab");
            if (prefab == null) return;
            GameObject hay = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            hay.name = "CowFeedHay";
            hay.transform.position = OnGround(-26f, 15f);
            hay.transform.localScale = Vector3.one * 0.8f;
        }

        private static void CreateCow(GameObject prefab, Transform root, CowPen pen, MeshCollider ground,
            Material cowMaterial, Material ropeMaterial, AnimatorController controller, int index)
        {
            Vector2 p = CowSpawns[index];
            GameObject cow = new GameObject("DairyCow_" + (index + 1));
            cow.transform.SetParent(root);
            cow.transform.position = OnGround(p.x, p.y);
            cow.transform.rotation = Quaternion.Euler(0f, (index * 73) % 360, 0f);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cow.transform);
            visual.name = "CowModel";
            visual.transform.localPosition = Vector3.zero;
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers) renderer.sharedMaterial = cowMaterial;
            if (renderers.Length > 0)
            {
                Bounds bounds = CombinedBounds(renderers);
                if (bounds.size.y > 0.01f) visual.transform.localScale *= CowHeight / bounds.size.y;
                // The imported model pivot is not guaranteed to be at hoof height.
                bounds = CombinedBounds(renderers);
                visual.transform.position += Vector3.up * (cow.transform.position.y - bounds.min.y);
            }
            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator == null) animator = visual.transform.GetChild(0).gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            CapsuleCollider collider = cow.AddComponent<CapsuleCollider>();
            collider.radius = 1.15f;
            collider.height = CowHeight;
            collider.center = Vector3.up * CowHeight * 0.5f;
            LineRenderer rope = cow.AddComponent<LineRenderer>();
            rope.sharedMaterial = ropeMaterial;
            rope.positionCount = 2;
            rope.startWidth = 0.055f;
            rope.endWidth = 0.045f;
            rope.useWorldSpace = true;
            rope.enabled = false;
            cow.AddComponent<CowAnimal>().Configure("cow-" + (index + 1), pen, ground, animator, rope);
        }

        private static Vector3 OnGround(float x, float z) => new Vector3(x, ReferenceLandscapeSetup.GroundHeightAt(x, z), z);
    }
}
