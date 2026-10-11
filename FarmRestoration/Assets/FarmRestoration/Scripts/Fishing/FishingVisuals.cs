using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Lightweight procedural rod, line and float; no imported fishing model required.</summary>
    public sealed class FishingVisuals : MonoBehaviour
    {
        private Transform player;
        private Transform rod;
        private Transform bobber;
        private LineRenderer line;
        private Material rodMaterial;
        private Material floatMaterial;
        private Material lineMaterial;
        public Vector3 WaterTarget { get; private set; }

        public static FishingVisuals Create(Transform owner, Vector3 waterTarget)
        {
            if (owner == null) return null;
            GameObject root = new GameObject("FishingVisuals");
            FishingVisuals visual = root.AddComponent<FishingVisuals>();
            visual.player = owner;
            visual.WaterTarget = waterTarget;
            visual.Build();
            return visual;
        }

        private void Build()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            rodMaterial = new Material(lit) { color = new Color(0.36f, 0.22f, 0.11f) };
            floatMaterial = new Material(lit) { color = new Color(0.9f, 0.23f, 0.1f) };
            lineMaterial = new Material(unlit) { color = new Color(0.94f, 0.94f, 0.8f) };
            GameObject rodObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rodObject.name = "FishingRod";
            rodObject.transform.SetParent(transform, false);
            rod = rodObject.transform;
            rod.localScale = new Vector3(0.035f, 0.85f, 0.035f);
            rodObject.GetComponent<Renderer>().sharedMaterial = rodMaterial;
            rodObject.GetComponent<Collider>().enabled = false;
            GameObject floatObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            floatObject.name = "FishingFloat";
            floatObject.transform.SetParent(transform, false);
            bobber = floatObject.transform;
            bobber.localScale = Vector3.one * 0.17f;
            floatObject.GetComponent<Renderer>().sharedMaterial = floatMaterial;
            floatObject.GetComponent<Collider>().enabled = false;
            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.018f;
            line.endWidth = 0.012f;
            line.sharedMaterial = lineMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        public void Refresh(FishingPhase phase, float elapsed, float progress)
        {
            if (player == null || rod == null || bobber == null) return;
            Vector3 forward = player.forward;
            Vector3 hand = player.position + player.right * 0.38f + Vector3.up * 1.25f + forward * 0.22f;
            Vector3 tip = hand + forward * 0.48f + Vector3.up * 1.55f;
            if (phase == FishingPhase.Casting)
            {
                float cast = Mathf.Clamp01(elapsed / 0.8f);
                tip = hand + forward * (0.15f + 1.05f * cast)
                    + Vector3.up * (1.8f - 0.55f * cast + 0.35f * Mathf.Sin(cast * Mathf.PI));
            }
            rod.position = Vector3.Lerp(hand, tip, 0.5f);
            rod.rotation = Quaternion.FromToRotation(Vector3.up, tip - hand);
            Vector3 target = WaterTarget;
            if (phase == FishingPhase.Casting)
            {
                float t = Mathf.Clamp01(elapsed / 0.8f);
                target = Vector3.Lerp(tip, WaterTarget, t) + Vector3.up * (1.35f * Mathf.Sin(t * Mathf.PI));
            }
            else if (phase == FishingPhase.Bite)
                target.y -= 0.10f + 0.08f * Mathf.Sin(elapsed * 18f);
            else if (phase == FishingPhase.Reeling)
                target = Vector3.Lerp(WaterTarget, hand, progress * 0.35f);
            else if (phase == FishingPhase.Caught)
                target = Vector3.Lerp(WaterTarget, tip, Mathf.Clamp01(elapsed / 0.8f));
            bobber.position = target;
            line.SetPosition(0, tip);
            line.SetPosition(1, target);
        }

        private void OnDestroy()
        {
            if (rodMaterial != null) Destroy(rodMaterial);
            if (floatMaterial != null) Destroy(floatMaterial);
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
