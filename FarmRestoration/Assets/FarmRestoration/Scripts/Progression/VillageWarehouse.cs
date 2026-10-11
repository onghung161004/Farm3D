using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class VillageWarehouse : MonoBehaviour, IInteractable
    {
        public static void EnsurePresent()
        {
            if (FindAnyObjectByType<VillageWarehouse>() != null) return;
            VillageOrderNpc trader = FindAnyObjectByType<VillageOrderNpc>();
            if (trader == null) return;
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "VillageWarehouse";
            crate.transform.SetParent(trader.transform.parent, true);
            Vector3 position = trader.transform.position + new Vector3(-8f, 0.45f, -4f);
            if (Physics.Raycast(position + Vector3.up * 6f, Vector3.down, out RaycastHit ground, 15f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position.y = ground.point.y + 0.45f;
            crate.transform.position = position;
            crate.transform.localScale = new Vector3(1.5f, 0.9f, 1.2f);
            Collider collider = crate.GetComponent<Collider>();
            collider.isTrigger = true;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                Material wood = new Material(shader);
                wood.color = new Color(0.48f, 0.30f, 0.17f);
                crate.GetComponent<Renderer>().sharedMaterial = wood;
            }
            crate.AddComponent<VillageWarehouse>();
            GameObject sign = new GameObject("Sign", typeof(TextMesh));
            sign.transform.SetParent(crate.transform, false);
            sign.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            TextMesh label = sign.GetComponent<TextMesh>();
            label.text = "WAREHOUSE  [E]";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 54;
            label.characterSize = 0.12f;
            label.color = new Color(1f, 0.92f, 0.69f);
        }

        public bool TryInteract(FarmTool tool) => FarmWarehouseView.Show();
        public string GetInteractionPrompt(FarmTool tool) => "Warehouse: inspect and sell stock  [E open]";
    }
}
