using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmRestoration.Editor
{
    public static class FarmDemoHudSetup
    {
        private const string FarmDemoScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";

        [MenuItem("Tools/Farm Restoration/Configure FarmDemo HUD")]
        public static void ConfigureHud()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FarmDemoScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command again.", "OK");
                return;
            }

            PlayerToolController controller = Object.FindAnyObjectByType<PlayerToolController>();
            FarmPlot plot = Object.FindAnyObjectByType<FarmPlot>();
            if (controller == null || plot == null)
            {
                Debug.LogError("HUD setup requires both PlayerToolController and FarmPlot in FarmDemo. The scene was left unchanged.");
                return;
            }

            GameObject hudObject = GameObject.Find("FarmHud");
            if (hudObject == null)
            {
                hudObject = new GameObject("FarmHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(hudObject, "Create Farm HUD");
                Canvas canvas = hudObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = hudObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                Undo.RecordObject(hudObject, "Configure Farm HUD");
            }

            FarmHud hud = hudObject.GetComponent<FarmHud>();
            if (hud == null)
            {
                hud = Undo.AddComponent<FarmHud>(hudObject);
            }
            else
            {
                Undo.RecordObject(hud, "Configure Farm HUD");
            }

            RectTransform panel = GetOrCreatePanel(hudObject.transform);
            FarmHudReadabilityStyle style = FarmHudReadabilityStyle.Default;
            TMP_Text tool = GetOrCreateLabel(panel, "ToolLabel", new Vector2(30f, -24f), style.ToolFontSize, Color.white, true, 62f);
            TMP_Text prompt = GetOrCreateLabel(panel, "PromptLabel", new Vector2(30f, -94f), style.ActionFontSize, new Color(1f, 0.82f, 0.2f), true, 48f);
            TMP_Text crops = GetOrCreateLabel(panel, "CropsLabel", new Vector2(30f, -150f), style.CropsFontSize, Color.white, true, 150f);
            TMP_Text controls = GetOrCreateLabel(panel, "ControlsLabel", new Vector2(30f, -322f), style.ControlsFontSize, new Color(0.9f, 0.95f, 1f), false, 66f);
            controls.text = style.ControlsText;

            hud.ConfigureLabels(tool, prompt, crops);
            hud.ConfigureSources(controller, plot);
            EditorUtility.SetDirty(hudObject);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = hudObject;
            Debug.Log("Farm HUD configured. Save the scene with Ctrl+S.", hud);
        }

        private static RectTransform GetOrCreatePanel(Transform parent)
        {
            Transform existing = parent.Find("Panel");
            GameObject panelObject;
            if (existing != null)
            {
                panelObject = existing.gameObject;
                Undo.RecordObject(panelObject, "Configure Farm HUD Panel");
            }
            else
            {
                panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(panelObject, "Create Farm HUD Panel");
                panelObject.transform.SetParent(parent, false);
            }

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            if (existing != null)
            {
                Undo.RecordObject(rect, "Configure Farm HUD Panel");
            }
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(28f, -28f);
            rect.sizeDelta = new Vector2(FarmHudReadabilityStyle.PanelWidth, FarmHudReadabilityStyle.PanelHeight);
            Image image = panelObject.GetComponent<Image>();
            if (image == null)
            {
                image = Undo.AddComponent<Image>(panelObject);
            }
            else if (existing != null)
            {
                Undo.RecordObject(image, "Configure Farm HUD Panel");
            }
            image.color = new Color(0.015f, 0.03f, 0.04f, 0.93f);
            return rect;
        }

        private static TMP_Text GetOrCreateLabel(RectTransform panel, string name, Vector2 position, float fontSize, Color color, bool isBold, float height)
        {
            Transform existing = panel.Find(name);
            TextMeshProUGUI label = existing == null
                ? CreateLabel(panel, name)
                : existing.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = Undo.AddComponent<TextMeshProUGUI>(existing.gameObject);
            }
            else
            {
                Undo.RecordObject(label, "Configure Farm HUD Label");
            }
            RectTransform rect = label.rectTransform;
            if (existing != null)
            {
                Undo.RecordObject(rect, "Configure Farm HUD Label");
            }
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(FarmHudReadabilityStyle.LabelWidth, height);
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Left;
            label.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            label.textWrappingMode = name == "ToolLabel" || name == "PromptLabel" ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            label.outlineWidth = 0.18f;
            label.outlineColor = new Color(0f, 0f, 0f, 0.92f);
            label.raycastTarget = false;
            return label;
        }

        private static TextMeshProUGUI CreateLabel(RectTransform panel, string name)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelObject, "Create Farm HUD Label");
            labelObject.transform.SetParent(panel, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            return label;
        }
    }
}
