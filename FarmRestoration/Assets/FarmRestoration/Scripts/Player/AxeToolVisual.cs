using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Creates a held axe with the same lit material style as the existing tools.</summary>
    public sealed class AxeToolVisual : MonoBehaviour
    {
        private Material wood;
        private Material steel;

        public static void Ensure(Transform holder)
        {
            if (holder == null || holder.Find("AxeTool") != null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Renderer source = holder.GetComponentInChildren<Renderer>(true);
                if (source != null && source.sharedMaterial != null) shader = source.sharedMaterial.shader;
            }
            if (shader == null) return;
            GameObject root = new GameObject("AxeTool", typeof(AxeToolVisual));
            root.transform.SetParent(holder, false);
            root.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            AxeToolVisual visual = root.GetComponent<AxeToolVisual>();
            visual.wood = new Material(shader) { color = new Color(0.40f, 0.23f, 0.10f) };
            visual.steel = new Material(shader) { color = new Color(0.65f, 0.72f, 0.75f) };
            Part(root.transform, "Handle", Vector3.zero, new Vector3(0.07f, 0.72f, 0.07f), visual.wood);
            Part(root.transform, "Head", new Vector3(0.09f, 0.27f, 0f), new Vector3(0.31f, 0.15f, 0.07f), visual.steel);
            Part(root.transform, "Blade", new Vector3(0.25f, 0.25f, 0f), new Vector3(0.08f, 0.22f, 0.035f), visual.steel);
        }

        private static void Part(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (wood != null) Destroy(wood);
                if (steel != null) Destroy(steel);
            }
            else
            {
                if (wood != null) DestroyImmediate(wood);
                if (steel != null) DestroyImmediate(steel);
            }
        }
    }
}
