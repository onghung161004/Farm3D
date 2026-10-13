using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmRestoration
{
    public sealed class FarmAudioSettingsView : MonoBehaviour
    {
        private static FarmAudioSettingsView instance;
        private Canvas canvas;
        private RectTransform overlay, panel, openButton;
        private PlayerMovement player;
        private Slider music, effects;
        private TMP_Text musicLabel, effectsLabel, musicMuteLabel, effectsMuteLabel, allMuteLabel;
        private bool ownsLock;
        public static bool IsOpen => instance != null && instance.overlay != null && instance.overlay.gameObject.activeSelf;

        public static void Ensure(FarmHud hud)
        {
            if (instance != null || hud == null) return;
            Canvas canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var go = new GameObject("AudioSettings", typeof(RectTransform), typeof(FarmAudioSettingsView));
            go.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            instance = go.GetComponent<FarmAudioSettingsView>();
            instance.Build(canvas);
        }

        private void Build(Canvas host)
        {
            canvas = host;
            player = FindAnyObjectByType<PlayerMovement>();
            Button button = QuestUiFactory.Button(transform, "AUDIO", Vector2.zero, new Vector2(140, 48), () => Show());
            openButton = (RectTransform)button.transform;
            openButton.anchorMin = openButton.anchorMax = openButton.pivot = Vector2.one;
            overlay = QuestUiFactory.Panel(transform, "AudioOverlay", Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            panel = QuestUiFactory.Panel(overlay, "AudioPanel", Vector2.zero, new Vector2(520, 370));
            AudioSettingsUi.Label(panel, "Title", new Vector2(0, 145)).text = "AUDIO SETTINGS";
            musicLabel = AudioSettingsUi.Label(panel, "MusicValue", new Vector2(0, 90));
            music = AudioSettingsUi.Slider(panel, new Vector2(-63, 45), value => FarmAudioSettings.SetVolume(FarmAudioGroup.Music, value));
            Button musicMute = QuestUiFactory.Button(panel, "Mute", new Vector2(166, 45), new Vector2(120, 48), ToggleMusic);
            musicMuteLabel = musicMute.GetComponentInChildren<TMP_Text>();
            effectsLabel = AudioSettingsUi.Label(panel, "EffectsValue", new Vector2(0, -15));
            effects = AudioSettingsUi.Slider(panel, new Vector2(-63, -60), value => FarmAudioSettings.SetVolume(FarmAudioGroup.Effects, value));
            Button effectsMute = QuestUiFactory.Button(panel, "Mute", new Vector2(166, -60), new Vector2(120, 48), ToggleEffects);
            effectsMuteLabel = effectsMute.GetComponentInChildren<TMP_Text>();
            Button allMute = QuestUiFactory.Button(panel, "Mute all", new Vector2(-111, -135), new Vector2(210, 48), ToggleAll);
            allMuteLabel = allMute.GetComponentInChildren<TMP_Text>();
            QuestUiFactory.Button(panel, "Close", new Vector2(111, -135), new Vector2(210, 48), Close);
            overlay.gameObject.SetActive(false);
            FarmAudioSettings.Changed += Refresh;
            Refresh(); Layout();
        }

        public static bool Show()
        {
            if (instance == null || FishingSession.IsFishing || FarmQuestView.IsOpen || VillageOrderDialogue.IsOpen
                || VillageResidentDialogue.IsOpen || FarmWarehouseView.IsOpen) return false;
            if (IsOpen) return true;
            if (instance.player == null) instance.player = FindAnyObjectByType<PlayerMovement>();
            instance.ownsLock = instance.player != null && !instance.player.ControlsLocked;
            if (instance.ownsLock) instance.player.SetControlsLocked(true);
            instance.transform.SetAsLastSibling();
            instance.overlay.gameObject.SetActive(true);
            instance.Refresh();
            return true;
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
            if (ownsLock && player != null) player.SetControlsLocked(false);
            ownsLock = false;
            FarmAudioSettings.Save();
        }

        private void ToggleMusic() => FarmAudioSettings.SetMuted(FarmAudioGroup.Music, !FarmAudioSettings.Snapshot().musicMuted);
        private void ToggleEffects() => FarmAudioSettings.SetMuted(FarmAudioGroup.Effects, !FarmAudioSettings.Snapshot().effectsMuted);
        private void ToggleAll() => FarmAudioSettings.SetAllMuted(!FarmAudioSettings.Snapshot().allMuted);

        private void Refresh()
        {
            AudioVolumeState state = FarmAudioSettings.Snapshot();
            music.SetValueWithoutNotify(state.music); effects.SetValueWithoutNotify(state.effects);
            musicLabel.text = "Background music: " + Mathf.RoundToInt(state.music * 100) + "%" + (state.musicMuted || state.allMuted ? " (OFF)" : "");
            effectsLabel.text = "SFX / actions: " + Mathf.RoundToInt(state.effects * 100) + "%" + (state.effectsMuted || state.allMuted ? " (OFF)" : "");
            musicMuteLabel.text = state.musicMuted ? "Unmute" : "Mute";
            effectsMuteLabel.text = state.effectsMuted ? "Unmute" : "Mute";
            allMuteLabel.text = state.allMuted ? "Unmute all" : "Mute all";
        }

        private void Layout()
        {
            if (canvas == null || openButton == null) return;
            float scale = Mathf.Max(0.01f, canvas.scaleFactor);
            Rect safe = Screen.safeArea;
            openButton.anchoredPosition = new Vector2(-18 - (Screen.width - safe.xMax) / scale, -150 - (Screen.height - safe.yMax) / scale);
            Rect rect = ((RectTransform)canvas.transform).rect;
            float fit = Mathf.Min(1f, (rect.width - 24) / 520f, (rect.height - 24) / 370f);
            panel.localScale = Vector3.one * Mathf.Max(0.1f, fit);
        }

        private void Update()
        {
            Layout();
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }
        private void OnApplicationPause(bool paused) { if (paused) FarmAudioSettings.Save(); }
        private void OnApplicationQuit() => FarmAudioSettings.Save();
        private void OnDisable() { Close(); }
        private void OnDestroy()
        {
            Close(); FarmAudioSettings.Changed -= Refresh;
            if (instance == this) instance = null;
        }
    }
}
