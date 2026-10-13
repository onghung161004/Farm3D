using FarmRestoration.World;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class VillageTraderRoutine : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float walkSpeed = 0.85f;
        [SerializeField, Min(0f)] private float waitSeconds = 2.5f;
        [SerializeField] private Vector3[] localStops =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(-2.4f, 0f, -0.8f),
            new Vector3(-1.2f, 0f, 1.5f)
        };

        private DayNightCycle clock;
        private Vector3 home;
        private int stopIndex;
        private float waitUntil;
        private bool waiting;
        private bool paused;
        private Animator animator;

        public bool IsPaused => paused;
        public int StopCount => localStops == null ? 0 : localStops.Length;
        public static bool IsNight(float timeOfDay) => timeOfDay < 0.22f || timeOfDay > 0.77f;

        public void ConfigureStops(Vector3[] stops)
        {
            if (stops == null || stops.Length == 0) return;
            localStops = (Vector3[])stops.Clone();
            stopIndex = 0;
            waiting = false;
        }

        private void Start()
        {
            home = transform.position;
            clock = FindAnyObjectByType<DayNightCycle>();
            animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
        }

        public void SetPaused(bool value) => paused = value;

        private void Update()
        {
            if (paused || localStops == null || localStops.Length == 0)
            {
                ApplyAnimation(false);
                return;
            }
            bool night = clock != null && IsNight(clock.TimeOfDay);
            if (night) waiting = false;
            Vector3 target = night ? home : home + localStops[stopIndex];
            Vector3 delta = target - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.04f)
            {
                ApplyAnimation(false);
                if (!night)
                {
                    if (!waiting)
                    {
                        waiting = true;
                        waitUntil = Time.time + waitSeconds;
                    }
                    else if (Time.time >= waitUntil)
                    {
                        stopIndex = (stopIndex + 1) % localStops.Length;
                        waiting = false;
                    }
                }
                return;
            }
            ApplyAnimation(true);
            Vector3 next = Vector3.MoveTowards(transform.position, new Vector3(target.x, transform.position.y, target.z), walkSpeed * Time.deltaTime);
            transform.position = next;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 5f);
        }

        private void ApplyAnimation(bool walking)
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            string state = walking ? "metarig|Walk" : "metarig|Idle";
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(state)) animator.Play(state);
        }
    }
}
