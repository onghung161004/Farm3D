using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmRestoration
{
    /// <summary>One persistent destination while using the axe; switches after the tree is felled.</summary>
    [DisallowMultipleComponent]
    public sealed class WoodcuttingGuide : MonoBehaviour
    {
        private PlayerToolController tools;
        private ChoppableTree target;
        private ChoppableTree routeTarget;
        private ChoppableTree[] trees = System.Array.Empty<ChoppableTree>();
        private MeshCollider ground;
        private MeshCollider bridge;
        private MeshFilter river;
        private TMP_Text label;
        private Image background;
        private LineRenderer trail;
        private Material material;
        private float nextScan;
        private float nextRoute;
        private Vector3 routeOrigin;
        private bool approximate;

        public static void Ensure(FarmHud hud, PlayerToolController controller)
        {
            if (hud == null || controller == null) return;
            Canvas canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            WoodcuttingGuide guide = canvas.GetComponentInChildren<WoodcuttingGuide>(true);
            if (guide == null)
            {
                GameObject root = new GameObject("WoodcuttingGuide", typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                RectTransform rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -155f);
                rect.sizeDelta = new Vector2(650f, 66f);
                guide = root.AddComponent<WoodcuttingGuide>();
                guide.Build();
            }
            guide.tools = controller;
        }

        public static bool IsAvailable(ChoppableTree tree)
            => tree != null && tree.isActiveAndEnabled && !tree.State.IsStump;

        public static ChoppableTree FindNearest(Vector3 origin, ChoppableTree[] candidates)
        {
            ChoppableTree best = null;
            float closest = float.PositiveInfinity;
            if (candidates == null) return null;
            foreach (ChoppableTree tree in candidates)
            {
                if (!IsAvailable(tree)) continue;
                float distance = Vector3.ProjectOnPlane(tree.transform.position - origin, Vector3.up).sqrMagnitude;
                if (distance >= closest) continue;
                closest = distance;
                best = tree;
            }
            return best;
        }

        private void Build()
        {
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0.10f, 0.20f, 0.12f, 0.92f);
            background.raycastTarget = false;
            background.enabled = false;
            GameObject text = new GameObject("TreeDirections", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(transform, false);
            RectTransform rect = (RectTransform)text.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 4f);
            rect.offsetMax = new Vector2(-12f, -4f);
            label = text.GetComponent<TMP_Text>();
            label.fontSize = 21f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.enabled = false;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                material = new Material(shader) { color = new Color(1f, 0.85f, 0.15f) };
                GameObject path = new GameObject("WoodcuttingGroundTrail", typeof(LineRenderer));
                path.transform.SetParent(transform, false);
                trail = path.GetComponent<LineRenderer>();
                trail.sharedMaterial = material;
                trail.useWorldSpace = true;
                trail.startWidth = 0.24f;
                trail.endWidth = 0.18f;
                trail.numCornerVertices = 3;
                trail.numCapVertices = 4;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.enabled = false;
            }
        }

        private void Update()
        {
            bool show = tools != null && tools.SelectedTool == FarmTool.Axe
                && !FishingSession.IsFishing && !VillageOrderDialogue.IsOpen
                && !VillageResidentDialogue.IsOpen && !FarmWarehouseView.IsOpen && !FarmQuestView.IsOpen && !FarmAudioSettingsView.IsOpen;
            background.enabled = show;
            label.enabled = show;
            if (!show)
            {
                if (trail != null) trail.enabled = false;
                target = null;
                routeTarget = null;
                return;
            }
            if (Time.unscaledTime >= nextScan)
            {
                trees = FindObjectsByType<ChoppableTree>();
                nextScan = Time.unscaledTime + 1f;
            }
            if (!IsAvailable(target)) target = FindNearest(tools.transform.position, trees);
            if (target == null)
            {
                label.text = "WOODCUTTING | No grown wood trees available\nTrees regrow after chopping";
                if (trail != null) trail.enabled = false;
                return;
            }
            float distance = Vector3.ProjectOnPlane(target.transform.position - tools.transform.position, Vector3.up).magnitude;
            UpdateRoute();
            label.text = distance < 2.6f
                ? "WOOD TREE | " + target.State.HitsRemaining + " hits left\nPress " + (Application.isMobilePlatform ? "ACTION" : "E") + " to chop"
                : "WOOD TREE | " + Mathf.CeilToInt(distance) + " m\n"
                    + (approximate ? "Thin line is a bearing only; avoid obstacles and cross at the bridge" : "Follow the gold ground trail with your axe");
        }

        private void UpdateRoute()
        {
            if (trail == null) return;
            if (ground == null) ground = GameObject.Find("WalkableValleyTerrain")?.GetComponent<MeshCollider>();
            if (river == null) river = GameObject.Find("RiverWater")?.GetComponent<MeshFilter>();
            if (bridge == null)
            {
                bridge = GameObject.Find("PolytopeBridgeWalkway")?.GetComponent<MeshCollider>();
                if (bridge == null) bridge = GameObject.Find("ImportedBridgeWalkway")?.GetComponent<MeshCollider>();
            }
            Vector3 origin = tools.transform.position;
            if (routeTarget != target || Time.time >= nextRoute || (origin - routeOrigin).sqrMagnitude > 25f)
            {
                Vector3 direction = Vector3.ProjectOnPlane(origin - target.transform.position, Vector3.up).normalized;
                if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
                Vector3 destination = target.transform.position + direction * 1.8f;
                Vector3[] route = CowLeadPathfinder.FindTo(origin, destination, ground, river,
                    tools.transform, target.transform, bridge);
                approximate = route.Length < 2;
                if (approximate) route = new[] { origin + Vector3.up * 1.1f, destination + Vector3.up * 1.1f };
                trail.startWidth = approximate ? 0.08f : 0.24f;
                trail.endWidth = approximate ? 0.08f : 0.18f;
                trail.positionCount = route.Length;
                trail.SetPositions(route);
                nextRoute = Time.time + 2f;
                routeOrigin = origin;
                routeTarget = target;
            }
            trail.enabled = true;
            trail.SetPosition(0, origin + Vector3.up * (approximate ? 1.1f : 0.35f));
        }

        private void OnDisable()
        {
            if (trail != null) trail.enabled = false;
        }

        private void OnDestroy()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
