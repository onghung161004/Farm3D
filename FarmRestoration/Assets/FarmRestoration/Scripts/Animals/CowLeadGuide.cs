using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmRestoration
{
    /// <summary>Shows where to lead a cow and when the pen accepts it.</summary>
    [DisallowMultipleComponent]
    public sealed class CowLeadGuide : MonoBehaviour
    {
        private static CowLeadGuide instance;
        private RectTransform panel;
        private Image background;
        private TMP_Text guidance;
        private TextMeshPro waypoint;
        private LineRenderer trail;
        private Material trailMaterial;
        private MeshCollider terrain;
        private MeshFilter river;
        private MeshCollider bridge;
        private Transform player;
        private CowPen routePen;
        private CowAnimal routeCow;
        private Vector3 routeOrigin;
        private float nextRouteAt;
        private bool approximateTrail;
        private CowAnimal activeCow;
        private CowAnimal lastCow;
        private float scanAt;
        private float completionUntil;

        public static void Ensure(FarmHud hud)
        {
            if (instance != null || hud == null) return;
            Canvas canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            GameObject root = new GameObject("CowLeadGuide", typeof(RectTransform), typeof(CowLeadGuide));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            instance = root.GetComponent<CowLeadGuide>();
            instance.Build();
        }

        private void Build()
        {
            GameObject box = new GameObject("LeadInstructions", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(transform, false);
            panel = (RectTransform)box.transform;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -62f);
            panel.sizeDelta = new Vector2(650f, 80f);
            background = box.GetComponent<Image>();
            background.color = new Color(0.15f, 0.20f, 0.15f, 0.94f);
            background.raycastTarget = false;

            GameObject text = new GameObject("GuideText", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(panel, false);
            RectTransform textRect = (RectTransform)text.transform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(620f, 72f);
            guidance = text.GetComponent<TMP_Text>();
            guidance.fontSize = 21f;
            guidance.alignment = TextAlignmentOptions.Center;
            guidance.color = Color.white;
            guidance.raycastTarget = false;

            GameObject marker = new GameObject("CowPenWaypoint", typeof(TextMeshPro));
            waypoint = marker.GetComponent<TextMeshPro>();
            waypoint.text = "COW PEN";
            waypoint.fontSize = 3f;
            waypoint.alignment = TextAlignmentOptions.Center;
            waypoint.color = new Color(1f, 0.89f, 0.35f);
            waypoint.rectTransform.sizeDelta = new Vector2(8f, 2f);
            marker.transform.localScale = Vector3.one * 0.25f;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                trailMaterial = new Material(shader) { color = new Color(1f, 0.79f, 0.20f, 0.92f) };
                GameObject path = new GameObject("CowLeadTrail", typeof(LineRenderer));
                trail = path.GetComponent<LineRenderer>();
                trail.sharedMaterial = trailMaterial;
                trail.useWorldSpace = true;
                trail.startWidth = 0.3f;
                trail.endWidth = 0.22f;
                trail.numCornerVertices = 3;
                trail.numCapVertices = 4;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.enabled = false;
            }
            panel.gameObject.SetActive(false);
            marker.SetActive(false);
        }

        private void Update()
        {
            if (Time.unscaledTime >= scanAt || (activeCow != null && !activeCow.IsFollowing))
            {
                scanAt = Time.unscaledTime + 0.25f;
                CowAnimal found = null;
                foreach (CowAnimal cow in FindObjectsByType<CowAnimal>())
                    if (cow.IsFollowing) { found = cow; break; }
                if (found == null && activeCow != null)
                {
                    lastCow = activeCow;
                    completionUntil = Time.unscaledTime + 3f;
                }
                activeCow = found;
            }

            if (activeCow != null)
            {
                CowPen pen = activeCow.HomePen;
                bool inside = pen != null && pen.Contains(activeCow.transform.position, 0.6f);
                if (inside)
                {
                    guidance.text = "COW IN PEN  |  Face the cow and press " + ActionName + " to release the lead";
                    background.color = new Color(0.08f, 0.35f, 0.19f, 0.95f);
                }
                else if (pen != null)
                {
                    float distance = Vector3.Distance(activeCow.transform.position, pen.GatePosition);
                    guidance.text = "LEADING COW  |  Pen gate: " + Mathf.CeilToInt(distance)
                        + " m " + DirectionTo(pen.GatePosition)
                        + "\nFollow the gold trail through the gate, then press " + ActionName + " on the cow";
                    background.color = new Color(0.17f, 0.24f, 0.20f, 0.95f);
                }
                else
                {
                    guidance.text = "LEADING COW  |  No pen assigned";
                    background.color = new Color(0.40f, 0.19f, 0.13f, 0.95f);
                }
                panel.gameObject.SetActive(true);
                if (pen != null)
                {
                    waypoint.transform.position = pen.GatePosition + Vector3.up * 2.5f;
                    if (Camera.main != null) waypoint.transform.rotation = Camera.main.transform.rotation;
                    waypoint.gameObject.SetActive(!inside);
                }
                else waypoint.gameObject.SetActive(false);
                if (pen != null && !inside)
                {
                    UpdateTrail(pen, activeCow);
                    if (approximateTrail)
                        guidance.text = "LEADING COW  |  The gold line points to the pen gate\nNo clear ground route here; avoid obstacles and use the bridge to cross water";
                    else if (trail == null || !trail.enabled)
                        guidance.text = "LEADING COW  |  Follow the COW PEN marker\nGround route unavailable; move to open ground";
                }
                else HideTrail();
                return;
            }

            waypoint.gameObject.SetActive(false);
            HideTrail();
            bool showCompletion = lastCow != null && Time.unscaledTime < completionUntil;
            panel.gameObject.SetActive(showCompletion);
            if (!showCompletion) return;
            bool penned = lastCow.IsPenned;
            guidance.text = penned
                ? "COW IN PEN  |  You can now feed it a carrot and collect milk later"
                : "LEAD DETACHED  |  Approach the cow and press " + ActionName + " to attach again";
            background.color = penned
                ? new Color(0.08f, 0.35f, 0.19f, 0.95f)
                : new Color(0.40f, 0.19f, 0.13f, 0.95f);
        }

        private void UpdateTrail(CowPen pen, CowAnimal cow)
        {
            if (trail == null) return;
            if (player == null)
            {
                PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
                if (movement != null) player = movement.transform;
            }
            if (terrain == null) terrain = GameObject.Find("WalkableValleyTerrain")?.GetComponent<MeshCollider>();
            if (river == null) river = GameObject.Find("RiverWater")?.GetComponent<MeshFilter>();
            if (bridge == null)
            {
                bridge = GameObject.Find("PolytopeBridgeWalkway")?.GetComponent<MeshCollider>();
                if (bridge == null) bridge = GameObject.Find("ImportedBridgeWalkway")?.GetComponent<MeshCollider>();
            }
            if (player == null || terrain == null) { HideTrail(); return; }
            if (Time.time >= nextRouteAt || routePen != pen || routeCow != cow
                || Vector3.Distance(routeOrigin, player.position) > 5f)
            {
                Vector3[] route = CowLeadPathfinder.Find(player.position, pen, terrain, river,
                    player, cow.transform, bridge);
                approximateTrail = route.Length < 2;
                if (approximateTrail)
                {
                    // Keep a visible bearing to the gate even if dense scenery defeats the ground grid.
                    trail.positionCount = 2;
                    trail.SetPosition(0, player.position + Vector3.up * 1.1f);
                    trail.SetPosition(1, pen.GatePosition + Vector3.up * 1.1f);
                    trail.startWidth = trail.endWidth = 0.12f;
                }
                else
                {
                    trail.positionCount = route.Length;
                    trail.SetPositions(route);
                    trail.startWidth = 0.3f;
                    trail.endWidth = 0.22f;
                }
                trail.enabled = true;
                routeOrigin = player.position;
                routePen = pen;
                routeCow = cow;
                nextRouteAt = Time.time + 2f;
            }
            if (trail.enabled && trail.positionCount > 0)
                trail.SetPosition(0, player.position + Vector3.up * (approximateTrail ? 1.1f : 0.35f));
        }

        private void HideTrail()
        {
            if (trail != null) trail.enabled = false;
            approximateTrail = false;
        }

        private static string ActionName => Application.isMobilePlatform ? "ACTION" : "E";

        private static string DirectionTo(Vector3 destination)
        {
            Camera camera = Camera.main;
            if (camera == null) return string.Empty;
            Vector3 toTarget = Vector3.ProjectOnPlane(destination - camera.transform.position, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            float ahead = Vector3.Dot(toTarget, forward);
            float sideways = Vector3.Dot(toTarget, right);
            if (ahead > Mathf.Abs(sideways) * 0.6f) return "AHEAD";
            if (-ahead > Mathf.Abs(sideways) * 0.6f) return "BEHIND";
            return sideways >= 0f ? "RIGHT" : "LEFT";
        }

        private void OnDestroy()
        {
            if (waypoint != null) Destroy(waypoint.gameObject);
            if (trail != null) Destroy(trail.gameObject);
            if (trailMaterial != null) Destroy(trailMaterial);
            if (instance == this) instance = null;
        }
    }
}
