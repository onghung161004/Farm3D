using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmRestoration
{
    /// <summary>Touch input layered over the existing PC controls; no duplicate gameplay logic.</summary>
    [DisallowMultipleComponent]
    public sealed class MobileGameControls : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IDragHandler, IPointerExitHandler
    {
        private const float JoystickRadius = 76f;
        private static MobileGameControls instance;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);
        private PlayerMovement movement;
        private PlayerFollowCamera followCamera;
        private PlayerToolController tools;
        private Canvas canvas;
        private RectTransform joystickBase;
        private RectTransform joystickKnob;
        private RectTransform actionButton;
        private RectTransform recipeButton;
        private GameObject controlsRoot;
        private int joystickPointer = int.MinValue;
        private int actionPointer = int.MinValue;
        private int lookTouchId = -1;
        private Vector2 joystickValue;
        private bool controlsVisible;

        public static void Ensure(FarmHud hud, PlayerToolController controller)
        {
            if (!Application.isMobilePlatform || hud == null || controller == null) return;
            if (instance != null) return;
            Canvas host = hud.GetComponentInParent<Canvas>();
            if (host == null) return;
            GameObject go = new GameObject("MobileGameControls", typeof(RectTransform), typeof(MobileGameControls));
            go.transform.SetParent(host.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            instance = go.GetComponent<MobileGameControls>();
            instance.canvas = host;
            instance.tools = controller;
            instance.movement = controller.GetComponent<PlayerMovement>();
            Camera mainCamera = Camera.main;
            instance.followCamera = mainCamera == null ? null : mainCamera.GetComponent<PlayerFollowCamera>();
            instance.Build();
        }

        private void Build()
        {
            controlsRoot = new GameObject("TouchButtons", typeof(RectTransform));
            controlsRoot.transform.SetParent(transform, false);
            RectTransform root = (RectTransform)controlsRoot.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            joystickBase = CreatePad(root, "MoveJoystick", 224f, new Color(0.04f, 0.12f, 0.12f, 0.55f));
            joystickKnob = CreatePad(joystickBase, "Knob", 88f, new Color(0.75f, 0.94f, 0.84f, 0.85f));
            joystickKnob.anchorMin = joystickKnob.anchorMax = new Vector2(0.5f, 0.5f);
            joystickKnob.anchoredPosition = Vector2.zero;
            joystickKnob.GetComponent<Image>().raycastTarget = false;
            CreateLabel(joystickBase, "MOVE", new Vector2(0f, -88f), new Vector2(180f, 30f), 22f);

            actionButton = CreatePad(root, "InteractButton", 132f, new Color(0.20f, 0.49f, 0.28f, 0.9f));
            CreateLabel(actionButton, "ACTION", Vector2.zero, new Vector2(130f, 42f), 26f);
            recipeButton = CreatePad(root, "RecipeButton", 112f, new Color(0.22f, 0.35f, 0.46f, 0.9f));
            CreateLabel(recipeButton, "RECIPE", Vector2.zero, new Vector2(110f, 36f), 21f);
            Button recipe = recipeButton.gameObject.AddComponent<Button>();
            recipe.onClick.AddListener(() => { if (tools != null) tools.TryCycleRecipe(); });
            PositionControls();
        }

        private static RectTransform CreatePad(Transform parent, string name, float size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        private static void CreateLabel(Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private void Update()
        {
            bool canControl = !FishingSession.IsFishing && !VillageOrderDialogue.IsOpen
                && !VillageResidentDialogue.IsOpen && !FarmWarehouseView.IsOpen && !FarmQuestView.IsOpen && !FarmAudioSettingsView.IsOpen;
            if (controlsVisible != canControl)
            {
                controlsVisible = canControl;
                controlsRoot.SetActive(canControl);
                if (!canControl) ReleaseInputs();
            }
            if (!canControl) return;
            PositionControls();
            if (tools != null)
                recipeButton.gameObject.SetActive(tools.CurrentPrompt != null
                    && tools.CurrentPrompt.Contains("[R change]"));
            ReadLookTouch();
        }

        private void PositionControls()
        {
            if (canvas == null || joystickBase == null) return;
            float scale = Mathf.Max(0.01f, canvas.scaleFactor);
            Rect safe = Screen.safeArea;
            float left = safe.xMin / scale;
            float right = (Screen.width - safe.xMax) / scale;
            float bottom = safe.yMin / scale;
            joystickBase.anchoredPosition = new Vector2(left + 156f, bottom + 175f);
            actionButton.anchorMin = actionButton.anchorMax = new Vector2(1f, 0f);
            actionButton.anchoredPosition = new Vector2(-right - 156f, bottom + 180f);
            recipeButton.anchorMin = recipeButton.anchorMax = new Vector2(1f, 0f);
            recipeButton.anchoredPosition = new Vector2(-right - 156f, bottom + 315f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!controlsVisible) return;
            if (eventData.pointerPressRaycast.gameObject == joystickBase.gameObject
                && joystickPointer == int.MinValue)
            {
                joystickPointer = eventData.pointerId;
                SetJoystick(eventData);
            }
            else if (eventData.pointerPressRaycast.gameObject == actionButton.gameObject
                && actionPointer == int.MinValue)
            {
                actionPointer = eventData.pointerId;
                if (tools != null) tools.SetMobileInteractHeld(true);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == joystickPointer) SetJoystick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData) => ReleasePointer(eventData.pointerId);
        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerId == actionPointer) ReleasePointer(eventData.pointerId);
        }

        private void SetJoystick(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBase,
                    eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            joystickValue = Vector2.ClampMagnitude(local / JoystickRadius, 1f);
            joystickKnob.anchoredPosition = joystickValue * JoystickRadius;
            if (movement != null) movement.SetMobileInput(joystickValue);
        }

        private void ReleasePointer(int pointerId)
        {
            if (pointerId == joystickPointer)
            {
                joystickPointer = int.MinValue;
                joystickValue = Vector2.zero;
                joystickKnob.anchoredPosition = Vector2.zero;
                if (movement != null) movement.SetMobileInput(Vector2.zero);
            }
            if (pointerId == actionPointer)
            {
                actionPointer = int.MinValue;
                if (tools != null) tools.SetMobileInteractHeld(false);
            }
        }

        private void ReadLookTouch()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null || followCamera == null) return;
            bool trackedTouchPresent = false;
            foreach (var touch in screen.touches)
            {
                if (!touch.press.isPressed) continue;
                int id = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();
                if (id == lookTouchId)
                {
                    trackedTouchPresent = true;
                    followCamera.AddOrbitDelta(touch.delta.ReadValue());
                }
                else if (lookTouchId < 0 && touch.press.wasPressedThisFrame
                    && position.x > Screen.width * 0.45f && !IsOverUi(position))
                {
                    lookTouchId = id;
                    trackedTouchPresent = true;
                }
            }
            if (!trackedTouchPresent) lookTouchId = -1;
        }

        private bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem events = EventSystem.current;
            if (events == null) return false;
            PointerEventData pointer = new PointerEventData(events) { position = screenPosition };
            uiHits.Clear();
            events.RaycastAll(pointer, uiHits);
            return uiHits.Count > 0;
        }

        private void ReleaseInputs()
        {
            lookTouchId = -1;
            ReleasePointer(joystickPointer);
            ReleasePointer(actionPointer);
        }

        private void OnDisable() => ReleaseInputs();
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
