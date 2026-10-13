using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Opt-in ground route; cow and axe guides take precedence. Never moves the player.</summary>
    public sealed class FarmQuestNavigation : MonoBehaviour
    {
        private FarmProgression game;
        private PlayerMovement player;
        private PlayerToolController tools;
        private Component[] destinations;
        private MeshCollider ground, bridge;
        private MeshFilter river;
        private LineRenderer line;
        private Material material;
        private bool tracking;
        private float nextRoute;
        private float nextNotice;

        public void Configure(FarmProgression progression)
        {
            game = progression;
            player = FindAnyObjectByType<PlayerMovement>();
            tools = FindAnyObjectByType<PlayerToolController>();
            destinations = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            ground = Named<MeshCollider>("WalkableValleyTerrain");
            bridge = Named<MeshCollider>("PolytopeBridgeWalkway");
            if (bridge == null) bridge = Named<MeshCollider>("ImportedBridgeWalkway");
            river = Named<MeshFilter>("RiverWater");
            var go = new GameObject("QuestGroundTrail");
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader != null) { material = new Material(shader); material.color = new Color(1, 0.77f, 0.2f); line.sharedMaterial = material; }
            line.useWorldSpace = true;
            line.widthMultiplier = 0.18f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
        }

        private static T Named<T>(string name) where T : Component
        { GameObject go = GameObject.Find(name); return go != null ? go.GetComponent<T>() : null; }

        public void Toggle()
        {
            tracking = !tracking;
            nextRoute = 0;
            if (!tracking && line != null) line.enabled = false;
            CowBreedingHud.Show(tracking ? "Quest tracking ON: follow the gold trail" : "Quest tracking OFF");
        }

        private void Update()
        {
            if (line == null) return;
            bool show = tracking && game != null && player != null && !FarmQuestView.IsOpen && !FarmAudioSettingsView.IsOpen
                && !FishingSession.IsFishing && !VillageOrderDialogue.IsOpen && !VillageResidentDialogue.IsOpen
                && !FarmWarehouseView.IsOpen && (tools == null || tools.SelectedTool != FarmTool.Axe);
            if (game != null) foreach (CowAnimal cow in game.Cows) if (cow != null && cow.IsFollowing) show = false;
            if (!show) { line.enabled = false; return; }
            if (Time.unscaledTime < nextRoute) return;
            nextRoute = Time.unscaledTime + 2f;
            Transform target = Target();
            if (target == null) { line.enabled = false; return; }
            Vector3 destination = target.position;
            // Interact from outside the object's footprint rather than routing into its collider.
            Vector3 towardPlayer = Vector3.ProjectOnPlane(player.transform.position - destination, Vector3.up).normalized;
            destination += towardPlayer * 1.8f;
            Vector3[] route = CowLeadPathfinder.FindTo(player.transform.position, destination, ground, river,
                player.transform, target, bridge);
            line.positionCount = route.Length;
            if (route.Length >= 2) line.SetPositions(route);
            line.enabled = route.Length >= 2;
            if (!line.enabled && Time.unscaledTime >= nextNotice)
            {
                nextNotice = Time.unscaledTime + 10f;
                CowBreedingHud.Show("No clear quest trail here; move to open ground and use the bridge.");
            }
        }

        private Transform Target()
        {
            FarmQuestProgress p = game.QuestProgress;
            FarmQuestContext c = game.QuestContext;
            if (FarmQuestRules.CanClaim(p, c) || p.claimedStage >= 6) return Nearest<FarmQuestBoard>();
            switch (p.claimedStage)
            {
                case 0:
                    if (p.talkedToVillager) return Nearest<FarmPlot>();
                    Transform resident = Nearest<VillageResidentNpc>();
                    return resident != null ? resident : Nearest<VillageOrderNpc>();
                case 1: return p.itemsProduced < 1 ? Nearest<FarmProductionStation>() : Nearest<VillageOrderNpc>();
                case 2: return Repair(1);
                case 3: return c.AdultCowsPenned < 2 ? Nearest<CowAnimal>() : Nearest<CowPen>();
                case 4: return p.fishCaught < 3 ? Nearest<FishingPondSpot>() : Nearest<FarmQuestBoard>();
                case 5: return !c.HouseRestored ? Repair(0) : Nearest<FarmQuestBoard>();
                default: return null;
            }
        }

        private Transform Repair(int index)
        {
            foreach (Component item in destinations) if (item is VillageRepairPoint point && point.RepairIndex == index) return point.transform;
            return null;
        }

        private Transform Nearest<T>() where T : Component
        {
            Transform best = null;
            float distance = float.PositiveInfinity;
            foreach (Component item in destinations)
            {
                if (item == null || !(item is T) || !item.gameObject.activeInHierarchy) continue;
                if (item is CowAnimal cow && (cow.IsCalf || cow.IsPenned)) continue;
                if (item is FarmPlot plot && !plot.IsUnlocked) continue;
                float d = (item.transform.position - player.transform.position).sqrMagnitude;
                if (d >= distance) continue;
                distance = d; best = item.transform;
            }
            return best;
        }

        private void OnDestroy()
        {
            if (line != null) { if (Application.isPlaying) Destroy(line.gameObject); else DestroyImmediate(line.gameObject); }
            if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
