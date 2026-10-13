using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider), typeof(FishingSession))]
    public sealed class FishingPondSpot : MonoBehaviour, IInteractable
    {
        private const float StartRange = 3.4f;
        private PlayerToolController player;
        private FishingSession session;
        private FishingHud hud;
        [SerializeField] private bool riverSpot;
        [SerializeField] private Vector3 riverWaterTarget;

        public float DistanceToPlayerSquared => player == null
            ? float.MaxValue : (player.transform.position - transform.position).sqrMagnitude;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInLoadedFarm()
        {
            Transform valley = GameObject.Find("IllustratedValleyMap")?.transform;
            Transform pond = valley?.Find("FishingPond");
            MeshCollider terrain = valley?.Find("WalkableValleyTerrain")?.GetComponent<MeshCollider>();
            if (pond != null && terrain != null) EnsureAt(pond, terrain);
            if (valley != null && terrain != null) EnsureRiverSpots(valley, terrain);
        }

        public static FishingPondSpot EnsureAt(Transform pond, MeshCollider terrain)
        {
            if (pond == null || terrain == null) return null;
            Transform spot = pond.Find("FishingSpot");
            if (spot == null)
            {
                spot = new GameObject("FishingSpot").transform;
                spot.SetParent(pond, false);
            }
            Vector2 center = FishingPondTerrain.Center;
            Vector3 local = new Vector3(center.x, 0f, center.y - FishingPondTerrain.WaterRadiusZ - 0.95f);
            Vector3 world = pond.TransformPoint(local);
            Ray ray = new Ray(world + Vector3.up * 100f, Vector3.down);
            if (terrain.Raycast(ray, out RaycastHit hit, 200f))
                local.y = pond.InverseTransformPoint(new Vector3(world.x, hit.point.y + 0.5f, world.z)).y;
            spot.localPosition = local;
            SphereCollider collider = spot.GetComponent<SphereCollider>();
            if (collider == null) collider = spot.gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 1.1f;
            if (spot.GetComponent<FishingSession>() == null) spot.gameObject.AddComponent<FishingSession>();
            FishingPondSpot component = spot.GetComponent<FishingPondSpot>();
            if (component == null) component = spot.gameObject.AddComponent<FishingPondSpot>();
            component.riverSpot = false;
            return component;
        }

        /// <summary>Places three reusable fishing interactions on dry riverbank, away from the bridge.</summary>
        public static bool EnsureRiverSpots(Transform valley, MeshCollider terrain)
        {
            MeshFilter river = valley?.Find("RiverWater")?.GetComponent<MeshFilter>();
            Mesh mesh = river?.sharedMesh;
            const int columns = 9;
            if (mesh == null || terrain == null || mesh.vertexCount % columns != 0) return false;
            int rows = mesh.vertexCount / columns;
            if (rows < 16) return false;
            Vector3[] vertices = mesh.vertices;
            float[] fractions = { 0.32f, 0.46f, 0.78f };
            bool changed = false;
            for (int i = 0; i < fractions.Length; i++)
            {
                int row = Mathf.RoundToInt((rows - 1) * fractions[i]);
                Vector3 center = river.transform.TransformPoint(vertices[row * columns + 4]);
                Vector3 left = river.transform.TransformPoint(vertices[row * columns]);
                Vector3 right = river.transform.TransformPoint(vertices[row * columns + 8]);
                Vector3 eastEdge = left.x > right.x ? left : right;
                Vector3 normal = Vector3.ProjectOnPlane(eastEdge - center, Vector3.up).normalized;
                Vector3 target = center + Vector3.up * 0.12f;
                if (!TryFindDryBank(terrain, center, normal, target.y, out Vector3 bank)
                    && !TryFindDryBank(terrain, center, -normal, target.y, out bank)) continue;

                string name = "RiverFishingSpot_" + (i + 1);
                Transform marker = valley.Find(name);
                if (marker == null)
                {
                    marker = new GameObject(name).transform;
                    marker.SetParent(valley, false);
                    changed = true;
                }
                if (Vector3.Distance(marker.position, bank) > 0.02f) { marker.position = bank; changed = true; }
                SphereCollider trigger = marker.GetComponent<SphereCollider>();
                if (trigger == null) { trigger = marker.gameObject.AddComponent<SphereCollider>(); changed = true; }
                trigger.isTrigger = true;
                trigger.radius = 1.1f;
                if (marker.GetComponent<FishingSession>() == null) { marker.gameObject.AddComponent<FishingSession>(); changed = true; }
                FishingPondSpot spot = marker.GetComponent<FishingPondSpot>();
                if (spot == null) { spot = marker.gameObject.AddComponent<FishingPondSpot>(); changed = true; }
                if (!spot.riverSpot || Vector3.Distance(spot.riverWaterTarget, target) > 0.02f) changed = true;
                spot.riverSpot = true;
                spot.riverWaterTarget = target;
                if (marker.Find("FishingSign") == null)
                {
                    GameObject sign = new GameObject("FishingSign", typeof(TextMesh));
                    sign.transform.SetParent(marker, false);
                    sign.transform.localPosition = Vector3.up * 1.35f;
                    TextMesh label = sign.GetComponent<TextMesh>();
                    label.text = Application.isMobilePlatform ? "FISHING [CAST]" : "FISHING [E]";
                    label.fontSize = 50;
                    label.characterSize = 0.08f;
                    label.anchor = TextAnchor.MiddleCenter;
                    label.alignment = TextAlignment.Center;
                    label.color = new Color(0.92f, 0.95f, 0.78f);
                    changed = true;
                }
            }
            return changed;
        }

        private static bool TryFindDryBank(MeshCollider terrain, Vector3 center, Vector3 direction,
            float waterHeight, out Vector3 bank)
        {
            foreach (float radius in new[] { 8.5f, 9.5f, 10.5f })
            {
                Vector3 point = center + direction * radius;
                Ray ray = new Ray(new Vector3(point.x, 100f, point.z), Vector3.down);
                if (!terrain.Raycast(ray, out RaycastHit hit, 200f) || hit.point.y < waterHeight + 0.15f) continue;
                bank = hit.point + Vector3.up * 0.5f;
                return true;
            }
            bank = default;
            return false;
        }

        private void Awake() => session = GetComponent<FishingSession>();

        private void Update()
        {
            if (player == null) player = FindAnyObjectByType<PlayerToolController>();
            if (hud == null) hud = FishingHud.Ensure();
            if (hud != null) hud.SetSpot(this, CanStart());
            Transform sign = riverSpot ? transform.Find("FishingSign") : null;
            if (sign != null && Camera.main != null) sign.rotation = Camera.main.transform.rotation;
        }

        private bool CanStart()
        {
            return player != null && session != null && !FishingSession.IsFishing
                && !VillageOrderDialogue.IsOpen && !VillageResidentDialogue.IsOpen
                && !FarmWarehouseView.IsOpen && !FarmQuestView.IsOpen && !FarmAudioSettingsView.IsOpen
                && DistanceToPlayerSquared <= StartRange * StartRange;
        }

        public bool TryBeginFromTouch() => TryInteract(FarmTool.Hoe);

        public bool TryInteract(FarmTool tool)
        {
            if (!CanStart()) return false;
            Vector3 waterTarget;
            if (riverSpot) waterTarget = riverWaterTarget;
            else
            {
                Vector2 center = FishingPondTerrain.Center;
                waterTarget = transform.parent.TransformPoint(new Vector3(center.x,
                    FishingPondTerrain.WaterLevel + 0.12f, center.y));
            }
            return session.TryBegin(player.GetComponent<PlayerMovement>(), waterTarget);
        }

        public string GetInteractionPrompt(FarmTool tool) => riverSpot
            ? "River fishing: cast rod [E]" : "Fishing pond: cast rod [E]";

        private void OnDisable()
        {
            if (hud != null) hud.ClearSpot(this);
            if (session != null) session.Cancel();
        }
    }
}
