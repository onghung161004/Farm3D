using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Uses the imported Cartoon Farm Crops models without changing plot gameplay.</summary>
    [RequireComponent(typeof(FarmPlot))]
    public sealed class CartoonCropVisuals : MonoBehaviour
    {
        [SerializeField] private GameObject dirtPile;
        [SerializeField] private GameObject pumpkinPlant;
        [SerializeField] private GameObject pumpkinFruit;
        [SerializeField] private GameObject carrotPlant;
        [SerializeField] private GameObject carrotFruit;
        [SerializeField] private GameObject tomatoPlant;
        [SerializeField] private GameObject tomatoFruit;

        private static readonly Dictionary<Material, Material> UrpMaterials = new Dictionary<Material, Material>();
        private readonly List<GameObject> createdModels = new List<GameObject>(7);

        private void Awake()
        {
            FarmPlot plot = GetComponent<FarmPlot>();
            GameObject plant;
            GameObject fruit;
            switch (plot.CropType)
            {
                case CropType.Carrot: plant = carrotPlant; fruit = carrotFruit; break;
                case CropType.Tomato: plant = tomatoPlant; fruit = tomatoFruit; break;
                default: plant = pumpkinPlant; fruit = pumpkinFruit; break;
            }

            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (dirtPile == null || plant == null || fruit == null || urp == null)
            {
                Debug.LogWarning("Cartoon crop prefabs or URP/Lit are missing; original crop visuals remain active.", this);
                return;
            }

            Transform seeded = transform.Find("SeededVisual");
            Transform watered = transform.Find("WateredVisual");
            Transform growing = transform.Find("GrowingVisual");
            Transform ready = transform.Find("ReadyToHarvestVisual");
            if (seeded == null || watered == null || growing == null || ready == null) return;

            float groundY = transform.TransformPoint(new Vector3(0f, 0.15f, 0f)).y;
            if (!CreateModel(dirtPile, seeded, urp, 0.55f, 0.18f, groundY, Vector3.zero)
                || !CreateModel(plant, watered, urp, 0.22f, 0.30f, groundY, Vector3.zero)
                || !CreateModel(plant, growing, urp, 0.55f, 0.72f, groundY, Vector3.zero)
                || !CreateModel(plant, ready, urp, 0.78f, 1.05f, groundY, Vector3.zero))
            {
                DiscardCreatedModels();
                Debug.LogWarning("A cartoon crop mesh has no renderer; original visuals remain active.", this);
                return;
            }

            float fruitY = plot.CropType == CropType.Tomato ? groundY + 0.38f : groundY;
            float fruitWidth = plot.CropType == CropType.Pumpkin ? 0.29f : 0.18f;
            bool fruitBuilt = CreateModel(fruit, ready, urp, fruitWidth, 0.28f, fruitY, new Vector3(0.18f, 0f, 0.08f));
            if (plot.CropType != CropType.Pumpkin)
                fruitBuilt &= CreateModel(fruit, ready, urp, fruitWidth, 0.28f, fruitY, new Vector3(-0.18f, 0f, -0.08f));
            if (!fruitBuilt)
            {
                DiscardCreatedModels();
                Debug.LogWarning("A cartoon crop fruit mesh has no renderer; original visuals remain active.", this);
                return;
            }

            HideOldModels(seeded);
            HideOldModels(watered);
            HideOldModels(growing);
            HideOldModels(ready);
        }

        private bool CreateModel(GameObject prefab, Transform state, Shader urp,
            float targetWidth, float targetHeight, float floorY, Vector3 worldOffset)
        {
            GameObject model = Instantiate(prefab, transform, false);
            model.name = "CartoonCrop_" + prefab.name;
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Destroy(model);
                return false;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            float height = bounds.size.y;
            if (width < 0.0001f || height < 0.0001f)
            {
                Destroy(model);
                return false;
            }

            float scale = Mathf.Min(targetWidth / width, targetHeight / height);
            model.transform.localScale *= scale;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 center = transform.position + worldOffset;
            model.transform.position += new Vector3(center.x - bounds.center.x, floorY - bounds.min.y, center.z - bounds.center.z);

            foreach (Renderer renderer in renderers)
            {
                Material[] source = renderer.sharedMaterials;
                Material[] converted = new Material[source.Length];
                for (int i = 0; i < source.Length; i++) converted[i] = GetUrpMaterial(source[i], urp);
                renderer.sharedMaterials = converted;
            }
            foreach (Collider collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
            model.transform.SetParent(state, true);
            createdModels.Add(model);
            return true;
        }

        private void DiscardCreatedModels()
        {
            foreach (GameObject model in createdModels) if (model != null) Destroy(model);
            createdModels.Clear();
        }

        private static Material GetUrpMaterial(Material source, Shader urp)
        {
            if (source == null) return null;
            if (UrpMaterials.TryGetValue(source, out Material cached) && cached != null) return cached;
            Material converted = new Material(urp) { name = "URP_" + source.name, enableInstancing = true };
            Texture albedo = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : source.mainTexture;
            converted.SetTexture("_BaseMap", albedo);
            converted.SetColor("_BaseColor", Color.white);
            UrpMaterials[source] = converted;
            return converted;
        }

        private static void HideOldModels(Transform state)
        {
            foreach (Transform child in state)
            {
                if (child.name == "Crop" || child.name == "Seed" || child.name.StartsWith("food_"))
                    child.gameObject.SetActive(false);
            }
        }
    }
}
