using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerMovement), typeof(PlayerToolController))]
    public sealed class FarmAudioFeedback : MonoBehaviour
    {
        private const string AudioPath = "FarmAudio/";
        private static readonly string[] GrassNames = { "footstep_grass_000", "footstep_grass_001", "footstep_grass_002" };
        private static readonly string[] WoodNames = { "footstep_wood_000", "footstep_wood_001", "footstep_wood_002" };

        [SerializeField, Min(0.2f)] private float stepDistance = 2.25f;
        [SerializeField, Min(0.1f)] private float minimumStepInterval = 0.45f;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.42f;
        [SerializeField, Range(0f, 1f)] private float actionVolume = 0.65f;

        private readonly RaycastHit[] surfaceHits = new RaycastHit[16];
        private CharacterController controller;
        private PlayerMovement movement;
        private PlayerToolController tools;
        private AudioSource footstepSource;
        private AudioSource actionSource;
        private AudioClip[] grassSteps;
        private AudioClip[] woodSteps;
        private AudioClip hoe, seeds, water, waterSource, harvest, production, order, repair;
        private Vector3 lastPosition;
        private float walkedDistance;
        private float lastStepTime = float.NegativeInfinity;
        private float lastWaterTime = float.NegativeInfinity;
        private int stepIndex;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            tools = GetComponent<PlayerToolController>();
            footstepSource = CreateSource("Footsteps");
            actionSource = CreateSource("Farm actions");
            grassSteps = LoadSteps(GrassNames);
            woodSteps = LoadSteps(WoodNames);
            hoe = Load("chop");
            seeds = Load("cloth1");
            waterSource = Load("loop_water_01");
            water = CreateWaterClip(waterSource);
            harvest = Load("dropLeather");
            production = Load("metalPot1");
            order = Load("handleCoins");
            repair = Load("creak1");
            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            tools.InteractionSucceeded += OnInteractionSucceeded;
            lastPosition = transform.position;
            walkedDistance = 0f;
        }

        private void OnDisable()
        {
            tools.InteractionSucceeded -= OnInteractionSucceeded;
            walkedDistance = 0f;
        }

        private void OnDestroy()
        {
            if (water != null && water != waterSource) Destroy(water);
        }

        private void LateUpdate()
        {
            Vector3 current = transform.position;
            Vector2 delta = new Vector2(current.x - lastPosition.x, current.z - lastPosition.z);
            lastPosition = current;
            float distance = delta.magnitude;
            if (!controller.isGrounded || !movement.IsMoving || distance < 0.001f || distance > 2f)
            {
                walkedDistance = 0f;
                return;
            }

            walkedDistance = Mathf.Min(walkedDistance + distance, stepDistance);
            if (walkedDistance < stepDistance || Time.time - lastStepTime < minimumStepInterval) return;
            walkedDistance -= stepDistance;
            AudioClip[] clips = GroundSurface() == FootstepSurface.Kind.Wood ? woodSteps : grassSteps;
            AudioClip clip = clips[stepIndex++ % clips.Length];
            if (clip == null) return;
            lastStepTime = Time.time;
            footstepSource.Stop();
            footstepSource.clip = clip;
            footstepSource.volume = footstepVolume;
            footstepSource.pitch = Random.Range(0.97f, 1.03f);
            footstepSource.Play();
        }

        private FootstepSurface.Kind GroundSurface()
        {
            Vector3 origin = transform.position + Vector3.up * 0.45f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, surfaceHits, 2f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            FootstepSurface.Kind result = FootstepSurface.Kind.Grass;
            for (int i = 0; i < count; i++)
            {
                Collider collider = surfaceHits[i].collider;
                if (collider == null || collider.transform.IsChildOf(transform) || surfaceHits[i].distance >= nearest)
                    continue;
                nearest = surfaceHits[i].distance;
                FootstepSurface marker = collider.GetComponentInParent<FootstepSurface>();
                result = marker != null ? marker.Surface : FootstepSurface.Kind.Grass;
            }
            return result;
        }

        private void OnInteractionSucceeded(FarmTool tool, IInteractable target)
        {
            AudioClip clip;
            if (target is FarmPlot)
            {
                switch (tool)
                {
                    case FarmTool.Hoe: clip = hoe; break;
                    case FarmTool.Seeds: clip = seeds; break;
                    case FarmTool.WateringCan: clip = water; break;
                    case FarmTool.Harvest: clip = harvest; break;
                    default: return;
                }
            }
            else if (target is FarmProductionStation) clip = production;
            else if (target is VillageOrderNpc) clip = order;
            else if (target is VillageRepairPoint) clip = repair;
            else return;

            if (clip == null) return;
            if (target is FarmPlot && tool == FarmTool.WateringCan)
            {
                if (Time.time - lastWaterTime < 0.75f) return;
                lastWaterTime = Time.time;
            }
            actionSource.pitch = Random.Range(0.97f, 1.03f);
            actionSource.PlayOneShot(clip, actionVolume);
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject emitter = new GameObject(sourceName);
            emitter.transform.SetParent(transform, false);
            AudioSource source = emitter.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.65f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = 20f;
            return source;
        }

        private static AudioClip Load(string name) => Resources.Load<AudioClip>(AudioPath + name);

        private static AudioClip[] LoadSteps(string[] names)
        {
            AudioClip[] clips = new AudioClip[names.Length];
            for (int i = 0; i < names.Length; i++) clips[i] = Load(names[i]);
            return clips;
        }

        // Trim a real water recording into a short, faded pour suitable for a single plot.
        private static AudioClip CreateWaterClip(AudioClip source)
        {
            if (source == null) return null;
            int frames = Mathf.Min(source.samples, Mathf.RoundToInt(source.frequency * 0.8f));
            if (frames <= 0) return source;
            int offset = Mathf.Min(Mathf.RoundToInt(source.frequency * 0.2f), source.samples - frames);
            float[] samples = new float[frames * source.channels];
            if (!source.GetData(samples, offset))
            {
                Debug.LogWarning("Could not trim watering audio; using the source clip.");
                return source;
            }
            int fadeFrames = Mathf.Max(1, Mathf.RoundToInt(source.frequency * 0.07f));
            for (int frame = 0; frame < frames; frame++)
            {
                float fadeIn = Mathf.Clamp01(frame / (float)fadeFrames);
                float fadeOut = Mathf.Clamp01((frames - 1 - frame) / (float)fadeFrames);
                float envelope = Mathf.Min(fadeIn, fadeOut);
                for (int channel = 0; channel < source.channels; channel++)
                    samples[frame * source.channels + channel] *= envelope;
            }
            AudioClip clip = AudioClip.Create("Water pour", frames, source.channels, source.frequency, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
