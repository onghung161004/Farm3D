using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmRestoration.Editor
{
    /// <summary>Builds a compact Minecraft-inspired item hotbar without replacing farm gameplay.</summary>
    public static class FarmInventoryHotbarSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string IconFolder = "Assets/FarmRestoration/Textures/Hotbar";
        private static readonly string[] Names = { "Hoe", "Seeds", "Water", "Harvest", "Pumpkin", "Carrot", "Tomato" };

        [MenuItem("Tools/Farm Restoration/Build Inventory Hotbar")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo", "Open the FarmDemo scene first.", "OK");
                return;
            }
            FarmHud hud = Object.FindAnyObjectByType<FarmHud>();
            PlayerToolController controller = Object.FindAnyObjectByType<PlayerToolController>();
            if (hud == null || controller == null)
            {
                EditorUtility.DisplayDialog("HUD missing", "Run Configure FarmDemo HUD once before building the inventory hotbar.", "OK");
                return;
            }
            Canvas canvas = hud.GetComponent<Canvas>();
            if (canvas == null) return;
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
            }

            Transform oldPanel = hud.transform.Find("Panel");
            if (oldPanel != null) oldPanel.gameObject.SetActive(false);
            Transform previous = hud.transform.Find("InventoryHotbar");
            if (previous != null) Object.DestroyImmediate(previous.gameObject);

            Sprite[] icons = CreateIcons();
            RectTransform hotbar = MakeRect(hud.transform, "InventoryHotbar");
            hotbar.anchorMin = hotbar.anchorMax = new Vector2(0.5f, 0f);
            hotbar.pivot = new Vector2(0.5f, 0f);
            hotbar.anchoredPosition = new Vector2(0f, 30f);
            hotbar.sizeDelta = new Vector2(854f, 154f);

            Image[] toolFrames = new Image[4];
            TMP_Text[] cropCounts = new TMP_Text[3];
            for (int i = 0; i < Names.Length; i++)
            {
                RectTransform slot = MakeRect(hotbar, "Slot_" + i + "_" + Names[i]);
                slot.anchorMin = slot.anchorMax = new Vector2(0f, 0f);
                slot.pivot = new Vector2(0f, 0f);
                slot.anchoredPosition = new Vector2(i * 116f + (i >= 4 ? 34f : 0f), 0f);
                slot.sizeDelta = new Vector2(100f, 112f);
                Image frame = slot.gameObject.AddComponent<Image>();
                frame.color = new Color(0.20f, 0.27f, 0.29f, 0.96f);
                frame.raycastTarget = false;
                Outline outline = slot.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.02f, 0.06f, 0.06f, 0.95f);
                outline.effectDistance = new Vector2(3f, -3f);
                if (i < 4) toolFrames[i] = frame;

                RectTransform inset = MakeRect(slot, "Inset");
                Stretch(inset, new Vector2(5f, 5f), new Vector2(-5f, -5f));
                Image insetImage = inset.gameObject.AddComponent<Image>();
                insetImage.color = new Color(0.08f, 0.11f, 0.12f, 0.95f);
                insetImage.raycastTarget = false;

                RectTransform iconRect = MakeRect(slot, "ItemIcon");
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.anchoredPosition = new Vector2(0f, -8f);
                iconRect.sizeDelta = new Vector2(72f, 72f);
                Image iconImage = iconRect.gameObject.AddComponent<Image>();
                iconImage.sprite = icons[i];
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                TMP_Text label = MakeText(slot, "ItemName", Names[i], 18f);
                SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(100f, 30f), new Vector2(0.5f, 0f));
                label.alignment = TextAlignmentOptions.Center;
                if (i < 4)
                {
                    TMP_Text key = MakeText(slot, "Shortcut", (i + 1).ToString(), 18f);
                    SetRect(key.rectTransform, new Vector2(10f, -7f), new Vector2(24f, 23f), new Vector2(0f, 1f));
                    key.color = new Color(1f, 0.83f, 0.36f);
                    slot.gameObject.AddComponent<HotbarToolButton>().Configure(controller, i + 1);
                    frame.raycastTarget = true;
                }
                else
                {
                    TMP_Text count = MakeText(slot, "Count", "0", 27f);
                    SetRect(count.rectTransform, new Vector2(-9f, 7f), new Vector2(40f, 32f), new Vector2(1f, 0f));
                    count.alignment = TextAlignmentOptions.Right;
                    count.color = Color.white;
                    cropCounts[i - 4] = count;
                }
            }

            TMP_Text prompt = MakeText(hotbar, "InteractionPrompt", "Move closer to a farm plot", 24f);
            SetRect(prompt.rectTransform, new Vector2(0f, 122f), new Vector2(854f, 34f), new Vector2(0.5f, 0f));
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.color = new Color(1f, 0.92f, 0.67f);
            prompt.outlineWidth = 0.2f;
            prompt.outlineColor = Color.black;

            hud.ConfigureHotbar(toolFrames, cropCounts, prompt);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = hotbar.gameObject;
            Debug.Log("Inventory hotbar created. Save FarmDemo with Ctrl+S.", hotbar.gameObject);
        }

        private static RectTransform MakeRect(Transform parent, string name)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            return item.GetComponent<RectTransform>();
        }

        private static TMP_Text MakeText(Transform parent, string name, string value, float size)
        {
            RectTransform rect = MakeRect(parent, name);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = pivot;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

        private static Sprite[] CreateIcons()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmRestoration/Textures")) AssetDatabase.CreateFolder("Assets/FarmRestoration", "Textures");
            if (!AssetDatabase.IsValidFolder(IconFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Textures", "Hotbar");
            Sprite[] sprites = new Sprite[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                string path = IconFolder + "/" + Names[i] + ".png";
                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
                {
                    Texture2D texture = DrawIcon(i);
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 64f;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprites;
        }

        private static Texture2D DrawIcon(int kind)
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
            void Box(int x0, int y0, int x1, int y1, Color32 color)
            {
                for (int y = Mathf.Max(0, y0); y <= Mathf.Min(63, y1); y++)
                    for (int x = Mathf.Max(0, x0); x <= Mathf.Min(63, x1); x++) pixels[y * 64 + x] = color;
            }
            void Disk(int cx, int cy, int radius, Color32 color)
            {
                for (int y = -radius; y <= radius; y++)
                    for (int x = -radius; x <= radius; x++)
                        if (x * x + y * y <= radius * radius && cx + x >= 0 && cx + x < 64 && cy + y >= 0 && cy + y < 64)
                            pixels[(cy + y) * 64 + cx + x] = color;
            }
            Color32 brown = new Color32(128, 75, 37, 255);
            Color32 green = new Color32(65, 166, 65, 255);
            Color32 steel = new Color32(191, 209, 211, 255);
            switch (kind)
            {
                case 0: Box(29, 7, 35, 49, brown); Box(18, 44, 44, 51, steel); Box(15, 41, 23, 46, steel); break;
                case 1: Box(15, 16, 49, 43, new Color32(185, 145, 76, 255)); Box(19, 39, 45, 46, brown); Disk(25, 29, 4, green); Disk(37, 26, 4, green); break;
                case 2: Box(17, 15, 46, 38, steel); Box(22, 37, 42, 47, steel); Box(42, 36, 55, 40, steel); Box(15, 24, 20, 34, brown); Disk(30, 22, 7, new Color32(65, 150, 221, 255)); break;
                case 3: Box(15, 17, 49, 33, brown); Box(20, 29, 44, 43, new Color32(216, 176, 96, 255)); Box(20, 14, 25, 47, brown); Box(40, 14, 45, 47, brown); break;
                case 4: Disk(31, 27, 20, new Color32(235, 122, 25, 255)); Disk(24, 27, 13, new Color32(248, 151, 36, 255)); Box(29, 44, 35, 52, brown); Box(32, 49, 44, 53, green); break;
                case 5: Box(27, 20, 37, 43, new Color32(242, 125, 31, 255)); Disk(32, 22, 8, new Color32(242, 125, 31, 255)); Box(28, 42, 33, 54, green); Box(20, 47, 31, 51, green); Box(33, 46, 44, 50, green); break;
                case 6: Disk(31, 28, 18, new Color32(219, 51, 45, 255)); Disk(25, 35, 5, new Color32(249, 105, 83, 255)); Box(29, 45, 35, 52, green); Box(22, 46, 42, 49, green); break;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
