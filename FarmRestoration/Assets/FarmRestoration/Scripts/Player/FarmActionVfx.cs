using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FarmActionVfx : MonoBehaviour
    {
        private const int PoolSize = 12;
        [SerializeField] private PlayerToolController tools;
        [SerializeField] private Material particleMaterial;
        private readonly ParticleSystem[] pool = new ParticleSystem[PoolSize];
        private int nextEmitter;

        public void ConfigureMaterial(Material material) => particleMaterial = material;

        private void OnEnable()
        {
            if (tools == null) tools = GetComponent<PlayerToolController>();
            CreatePool();
            if (tools != null) tools.InteractionSucceeded += OnInteractionSucceeded;
        }

        private void OnDisable()
        {
            if (tools != null) tools.InteractionSucceeded -= OnInteractionSucceeded;
        }

        private void OnDestroy()
        {
            foreach (ParticleSystem emitter in pool)
                if (emitter != null) Destroy(emitter.gameObject);
        }

        private void OnInteractionSucceeded(FarmTool tool, IInteractable target)
        {
            if (!(target is FarmPlot plot)) return;
            ParticleSystem emitter = pool[nextEmitter++ % PoolSize];
            if (emitter == null) return;

            Color color;
            int count;
            switch (tool)
            {
                case FarmTool.Hoe: color = new Color(0.46f, 0.28f, 0.12f); count = 9; break;
                case FarmTool.Seeds: color = new Color(0.50f, 0.82f, 0.25f); count = 6; break;
                case FarmTool.WateringCan: color = new Color(0.25f, 0.72f, 1f); count = 13; break;
                case FarmTool.Harvest: color = new Color(1f, 0.78f, 0.20f); count = 12; break;
                default: return;
            }

            emitter.transform.position = plot.transform.position + Vector3.up * 0.25f;
            emitter.Clear();
            emitter.Play();
            var parameters = new ParticleSystem.EmitParams { startColor = color };
            emitter.Emit(parameters, count);
        }

        private void CreatePool()
        {
            if (pool[0] != null) return;
            if (particleMaterial == null)
            {
                Debug.LogWarning("Action VFX material is missing; farming remains available.", this);
                return;
            }
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject effect = new GameObject("Farm Action Burst " + i, typeof(ParticleSystem));
                ParticleSystem emitter = effect.GetComponent<ParticleSystem>();
                emitter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = emitter.main;
                main.playOnAwake = false;
                main.loop = false;
                main.startLifetime = 0.55f;
                main.startSpeed = 1.3f;
                main.startSize = 0.09f;
                main.gravityModifier = 0.8f;
                main.maxParticles = 20;
                var emission = emitter.emission;
                emission.rateOverTime = 0f;
                var shape = emitter.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.22f;
                ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = particleMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                pool[i] = emitter;
            }
        }
    }
}
