using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmRestoration
{
    public sealed class FarmQuestView : MonoBehaviour
    {
        private static FarmQuestView instance;
        private FarmProgression game;
        private RectTransform modal;
        private TMP_Text tracker, chapters, objectives;
        private Button claim, donate;
        private PlayerMovement player;
        private FarmQuestNavigation navigation;
        private FarmQuestBoard board;
        private bool ownsMovementLock;
        public static bool IsOpen => instance != null && instance.modal != null && instance.modal.gameObject.activeSelf;

        public static void Ensure(FarmProgression game, FarmHud hud)
        {
            if (instance != null || game == null || hud == null) return;
            Canvas canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var host = new GameObject("VillageQuests", typeof(RectTransform), typeof(FarmQuestView));
            host.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)host.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            instance = host.GetComponent<FarmQuestView>();
            instance.Build(game);
        }

        public static bool Show()
        {
            if (instance == null || !instance.game.IsReady || FishingSession.IsFishing || FarmAudioSettingsView.IsOpen
                || VillageOrderDialogue.IsOpen || VillageResidentDialogue.IsOpen || FarmWarehouseView.IsOpen) return false;
            instance.Open();
            return true;
        }

        private void Build(FarmProgression progression)
        {
            game = progression;
            player = FindAnyObjectByType<PlayerMovement>();
            board = FindAnyObjectByType<FarmQuestBoard>();
            navigation = game.gameObject.AddComponent<FarmQuestNavigation>();
            navigation.Configure(game);
            RectTransform hud = QuestUiFactory.Panel(transform, "QuestTracker", Vector2.zero, new Vector2(360, 110));
            hud.anchorMin = hud.anchorMax = hud.pivot = new Vector2(0, 1);
            hud.anchoredPosition = new Vector2(18, -68);
            tracker = QuestUiFactory.Text(hud, "Objective", new Vector2(0, 18), new Vector2(338, 66), 20);
            QuestUiFactory.Button(hud, "QUESTS", new Vector2(-86, -32), new Vector2(155, 34), () => Show());
            QuestUiFactory.Button(hud, "TRACK", new Vector2(86, -32), new Vector2(155, 34), ToggleTracking);
            modal = QuestUiFactory.Panel(transform, "QuestPanel", Vector2.zero, new Vector2(780, 460));
            QuestUiFactory.Text(modal, "Title", new Vector2(0, 192), new Vector2(730, 40), 28).text = "VILLAGE RESTORATION";
            chapters = QuestUiFactory.Text(modal, "Chapters", new Vector2(-213, 0), new Vector2(310, 305), 21);
            objectives = QuestUiFactory.Text(modal, "Objectives", new Vector2(163, 0), new Vector2(400, 305), 23);
            claim = QuestUiFactory.Button(modal, "Claim reward", new Vector2(-264, -192), new Vector2(180, 48), () => game.TryClaimQuest());
            donate = QuestUiFactory.Button(modal, "Give 1 fish", new Vector2(-64, -192), new Vector2(170, 48), () => game.TryGiveQuestFish());
            QuestUiFactory.Button(modal, "Track", new Vector2(123, -192), new Vector2(150, 48), ToggleTracking);
            QuestUiFactory.Button(modal, "Close", new Vector2(288, -192), new Vector2(145, 48), Close);
            modal.gameObject.SetActive(false);
            game.Changed += Refresh;
            Refresh();
        }

        private void Open()
        {
            if (IsOpen) return;
            ownsMovementLock = player != null && !player.ControlsLocked;
            if (ownsMovementLock) player.SetControlsLocked(true);
            modal.gameObject.SetActive(true);
            modal.SetAsLastSibling();
            Refresh();
        }

        private void Close()
        {
            if (modal != null) modal.gameObject.SetActive(false);
            if (ownsMovementLock && player != null) player.SetControlsLocked(false);
            ownsMovementLock = false;
        }

        private void ToggleTracking() { if (navigation != null) navigation.Toggle(); }

        private void Refresh()
        {
            int stage = game.QuestProgress.claimedStage;
            string title = FarmQuestRules.Title(stage);
            string goals = FarmQuestRules.Objectives(game.QuestProgress, game.QuestContext);
            string nextGoal = "Open QUESTS for your next goal.";
            foreach (string goal in goals.Split('\n')) if (goal.StartsWith("[ ] ")) { nextGoal = goal.Substring(4); break; }
            tracker.text = stage < 6 ? "VILLAGE RESTORATION " + stage + "/6\n" + (FarmQuestRules.CanClaim(game.QuestProgress, game.QuestContext) ? "Ready! Open QUESTS to claim." : nextGoal) : "VILLAGE RESTORED 6/6\nThank you! Keep enjoying your farm.";
            chapters.text = "";
            for (int i = 0; i < 6; i++) chapters.text += (i < stage ? "[DONE] " : i == stage ? "> " : "  ") + (i + 1) + ". " + FarmQuestRules.Title(i) + "\n\n";
            objectives.text = title + "\n\n" + goals + (stage < 6 ? "\n\nReward: " + FarmQuestRules.Reward(stage) + " coins" : "");
            bool atBoard = board != null && player != null && board.IsNearby(player.transform.position);
            if ((stage == 4 || stage == 5) && !atBoard) objectives.text += "\nGo to the wooden quest board near the village well.";
            claim.interactable = game.IsReady && FarmQuestRules.CanClaim(game.QuestProgress, game.QuestContext) && (stage != 5 || atBoard);
            donate.interactable = game.IsReady && atBoard && stage == 4 && game.QuestProgress.fishDelivered < 2 && game.FishCount > 0;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.qKey.wasPressedThisFrame) { if (IsOpen) Close(); else Show(); }
            if (keyboard != null && IsOpen && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        private void OnDisable() { Close(); }
        private void OnDestroy()
        {
            Close();
            if (game != null) game.Changed -= Refresh;
            if (navigation != null) { if (Application.isPlaying) Destroy(navigation); else DestroyImmediate(navigation); }
            if (instance == this) instance = null;
        }
    }
}
