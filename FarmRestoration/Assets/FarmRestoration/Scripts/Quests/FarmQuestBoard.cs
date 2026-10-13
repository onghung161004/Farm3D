using TMPro;
using UnityEngine;

namespace FarmRestoration
{
    public sealed class FarmQuestBoard : MonoBehaviour, IInteractable
    {
        private FarmProgression game;
        private TMP_Text label;
        private Transform supplies, bench;
        private Material ownedWood;
        public bool IsNearby(Vector3 position) => Vector3.ProjectOnPlane(position - transform.position, Vector3.up).sqrMagnitude <= 16f;

        public static void Ensure(FarmProgression game)
        {
            if (game == null || FindAnyObjectByType<FarmQuestBoard>() != null) return;
            GameObject well = GameObject.Find("VillageWell");
            VillageOrderNpc trader = FindAnyObjectByType<VillageOrderNpc>();
            if (well == null && trader == null) return;
            Transform landmark = well != null ? well.transform : trader.transform;
            Transform root = RestorationProps.Group(landmark.parent, "VillageQuestBoard");
            root.position = landmark.position + new Vector3(3.5f, 0f, -3.5f);
            root.rotation = landmark.rotation;
            GameObject groundObject = GameObject.Find("WalkableValleyTerrain");
            MeshCollider ground = groundObject != null ? groundObject.GetComponent<MeshCollider>() : null;
            RestorationProps.Ground(root, ground);
            FarmQuestBoard board = root.gameObject.AddComponent<FarmQuestBoard>();
            board.game = game;
            Material wood = RestorationProps.WoodFrom(landmark);
            if (wood == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null) { board.ownedWood = new Material(shader); board.ownedWood.color = new Color(0.35f, 0.22f, 0.13f); wood = board.ownedWood; }
            }
            RestorationProps.Part(root, "Post", new Vector3(0, 0.9f, 0), new Vector3(0.14f, 1.8f, 0.14f), wood);
            RestorationProps.Part(root, "WoodBoard", new Vector3(0, 1.5f, 0), new Vector3(2.5f, 1.05f, 0.12f), wood);
            BoxCollider interact = root.gameObject.AddComponent<BoxCollider>();
            interact.center = new Vector3(0, 1.1f, 0); interact.size = new Vector3(2.5f, 2.2f, 0.5f); interact.isTrigger = true;
            var text = new GameObject("QuestBoardLabel", typeof(TextMeshPro));
            text.transform.SetParent(root, false);
            text.transform.localPosition = new Vector3(0, 1.55f, -0.08f);
            text.transform.localRotation = Quaternion.Euler(0, 180, 0);
            board.label = text.GetComponent<TMP_Text>();
            board.label.rectTransform.sizeDelta = new Vector2(2.3f, 0.9f);
            board.label.fontSize = 3;
            board.label.color = new Color(1, 0.92f, 0.69f);
            board.label.alignment = TextAlignmentOptions.Center;
            board.supplies = RestorationProps.Group(root, "RestoredSupplies");
            for (int i = 0; i < 2; i++) RestorationProps.Part(board.supplies, "WoodCrate", new Vector3(-1.65f, 0.35f, i * 0.8f), new Vector3(0.65f, 0.7f, 0.65f), wood);
            board.bench = RestorationProps.Group(root, "RestoredVillageBench");
            RestorationProps.Part(board.bench, "Seat", new Vector3(2.5f, 0.5f, 0), new Vector3(2, 0.13f, 0.6f), wood);
            RestorationProps.Part(board.bench, "Back", new Vector3(2.5f, 0.9f, 0.3f), new Vector3(2, 0.5f, 0.1f), wood);
            for (int i = 0; i < 2; i++) RestorationProps.Part(board.bench, "Leg", new Vector3(1.8f + i * 1.4f, 0.25f, 0), new Vector3(0.16f, 0.5f, 0.5f), wood);
            game.Changed += board.Refresh;
            board.Refresh();
        }

        private void Refresh()
        {
            int stage = game.QuestProgress.claimedStage;
            label.text = stage >= 6 ? "VILLAGE RESTORED\nThank you!" : "VILLAGE QUESTS\n" + stage + "/6 complete";
            supplies.gameObject.SetActive(stage >= 2);
            bench.gameObject.SetActive(stage >= 6);
        }

        public bool TryInteract(FarmTool tool) => FarmQuestView.Show();
        public string GetInteractionPrompt(FarmTool tool) => "Village quests: objectives and rewards [E / ACTION]";
        private void OnDestroy()
        {
            if (game != null) game.Changed -= Refresh;
            if (ownedWood != null) { if (Application.isPlaying) Destroy(ownedWood); else DestroyImmediate(ownedWood); }
        }
    }
}
