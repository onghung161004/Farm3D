using TMPro;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class LumberWorkshop : MonoBehaviour, IInteractable
    {
        public const int WoodCost = 20;
        public const int CoinCost = 30;
        public const int UpgradedCapacity = 12;
        private CowPen pen;
        private Material material;
        private Transform sign;
        private TMP_Text signLabel;

        public static void Ensure(CowPen target)
        {
            if (target == null || target.transform.Find("PenUpgradeWorkbench") != null) return;
            GameObject bench = new GameObject("PenUpgradeWorkbench", typeof(BoxCollider));
            bench.transform.SetParent(target.transform, false);
            Vector3 position = target.GatePosition + target.transform.right * 2.2f - target.transform.forward * 2f;
            MeshCollider ground = GameObject.Find("WalkableValleyTerrain")?.GetComponent<MeshCollider>();
            if (ground != null && ground.Raycast(new Ray(position + Vector3.up * 20f, Vector3.down), out RaycastHit hit, 40f))
                position.y = hit.point.y;
            bench.transform.position = position;
            BoxCollider interaction = bench.GetComponent<BoxCollider>();
            interaction.center = Vector3.up * 0.45f;
            interaction.size = new Vector3(1.2f, 0.9f, 0.7f);
            interaction.isTrigger = true;
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "WorkbenchModel";
            model.transform.SetParent(bench.transform, false);
            model.transform.localPosition = Vector3.up * 0.45f;
            model.transform.localScale = new Vector3(1.2f, 0.9f, 0.7f);
            model.GetComponent<Collider>().enabled = false;
            LumberWorkshop workshop = bench.AddComponent<LumberWorkshop>();
            workshop.pen = target;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                workshop.material = new Material(shader) { color = new Color(0.44f, 0.27f, 0.13f) };
                model.GetComponent<Renderer>().sharedMaterial = workshop.material;
            }
            if (!Application.isPlaying) return;
            GameObject sign = new GameObject("Sign", typeof(TextMeshPro));
            sign.transform.SetParent(bench.transform, false);
            sign.transform.localPosition = Vector3.up * 1.5f;
            workshop.sign = sign.transform;
            TMP_Text label = sign.GetComponent<TextMeshPro>();
            workshop.signLabel = label;
            label.text = "PEN UPGRADE\n20 WOOD + 30 COINS\n12 COWS [E / ACTION]";
            label.fontSize = 2.4f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(7f, 2f);
        }

        public bool TryInteract(FarmTool tool) => FarmProgression.Instance != null
            && FarmProgression.Instance.TryUpgradeCowPen(pen);

        public string GetInteractionPrompt(FarmTool tool)
        {
            FarmProgression game = FarmProgression.Instance;
            if (game == null) return "Pen upgrade is loading";
            return game.CowPenUpgraded ? "Pen expanded: space for 12 cows"
                : "Upgrade pen: 20 wood + 30 coins (you have " + game.WoodCount + " wood, " + game.Coins + " coins) [E]";
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (sign != null && camera != null) sign.rotation = camera.transform.rotation;
            if (signLabel != null && FarmProgression.Instance != null && FarmProgression.Instance.CowPenUpgraded)
                signLabel.text = "PEN EXPANDED\n12 COWS\nUPGRADE COMPLETE";
        }

        private void OnDestroy()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
