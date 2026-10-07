using System;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class CowAnimal : MonoBehaviour, IInteractable
    {
        private const float MilkDelaySeconds = 120f;
        private const float FollowSpeed = 2.8f;
        [SerializeField] private string cowId;
        [SerializeField] private CowPen pen;
        [SerializeField] private MeshCollider valleyGround;
        [SerializeField] private Animator animator;
        [SerializeField] private LineRenderer lead;

        private Transform player;
        private Vector3 home;
        private Vector3 wanderTarget;
        private float nextWanderTime;
        private float eatingUntil;
        private string currentAnimation;
        private bool penned;
        private bool following;
        private long milkReadyUtcTicks;

        public string CowId => cowId;
        public bool IsPenned => penned;
        public bool IsFollowing => following;
        public bool CanFeed => penned && milkReadyUtcTicks == 0;
        public event Action<CowAnimal> StateChanged;

        public void Configure(string id, CowPen homePen, MeshCollider ground, Animator cowAnimator, LineRenderer rope)
        {
            cowId = id;
            pen = homePen;
            valleyGround = ground;
            animator = cowAnimator;
            lead = rope;
            home = transform.position;
            if (lead != null) lead.enabled = false;
        }

        private void Awake()
        {
            home = transform.position;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (lead == null) lead = GetComponent<LineRenderer>();
            if (lead != null) lead.enabled = false;
        }

        private void Update()
        {
            if (player == null)
            {
                PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
                if (movement != null) player = movement.transform;
            }

            Vector3 target = transform.position;
            float speed = 0f;
            if (following && player != null)
            {
                Vector3 behind = player.position - player.forward * 1.7f;
                if (Vector3.Distance(transform.position, behind) > 1.35f)
                {
                    target = behind;
                    speed = FollowSpeed;
                }
                // A long lead detaches instead of teleporting a cow across the river or scenery.
                if (Vector3.Distance(transform.position, player.position) > 13f) StopFollowing();
            }
            else if (Time.time >= nextWanderTime)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * (penned ? 2.2f : 3.5f);
                wanderTarget = home + new Vector3(offset.x, 0f, offset.y);
                if (penned && pen != null) wanderTarget = pen.ClampInside(wanderTarget, 1.3f);
                nextWanderTime = Time.time + UnityEngine.Random.Range(5f, 9f);
            }
            if (!following && Vector3.Distance(transform.position, wanderTarget) > 0.4f)
            {
                target = wanderTarget;
                speed = 0.65f;
            }
            bool moved = MoveTowards(target, speed);
            ShowAnimation(Time.time < eatingUntil ? "Eating" : moved ? "Walk" : "Idle");
            UpdateLead();
        }

        private bool MoveTowards(Vector3 target, float speed)
        {
            if (speed <= 0f) return false;
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return false;
            Vector3 next = transform.position + Vector3.ClampMagnitude(flat, speed * Time.deltaTime);
            if (penned && pen != null) next = pen.ClampInside(next, 1.3f);
            if (!penned && pen != null && !pen.AllowsStep(transform.position, next)) return false;
            if (valleyGround != null)
            {
                Ray ray = new Ray(next + Vector3.up * 12f, Vector3.down);
                if (!valleyGround.Raycast(ray, out RaycastHit hit, 30f)
                    || Mathf.Abs(hit.point.y - transform.position.y) > 1.25f) return false;
                next.y = hit.point.y;
            }
            transform.position = next;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(flat.normalized), 1f - Mathf.Exp(-7f * Time.deltaTime));
            return true;
        }

        private void ShowAnimation(string state)
        {
            if (animator == null || animator.runtimeAnimatorController == null || currentAnimation == state) return;
            currentAnimation = state;
            animator.CrossFadeInFixedTime(state, 0.18f);
        }

        private void UpdateLead()
        {
            if (lead == null) return;
            lead.enabled = following && player != null;
            if (!lead.enabled) return;
            lead.SetPosition(0, player.position + Vector3.up * 1.05f);
            lead.SetPosition(1, transform.position + Vector3.up * 1.7f);
        }

        public bool TryInteract(FarmTool tool)
        {
            if (following)
            {
                StopFollowing();
                if (pen != null && pen.Contains(transform.position, 0.6f))
                {
                    penned = true;
                    home = pen.ClampInside(transform.position, 1.2f);
                    transform.position = home;
                }
                StateChanged?.Invoke(this);
                return true;
            }
            if (penned)
            {
                FarmProgression game = FarmProgression.Instance;
                if (game == null) return false;
                if (CanMilk(DateTime.UtcNow)) return game.TryMilkCow(this);
                return CanFeed && game.TryFeedCow(this);
            }
            if (player == null) return false;
            following = true;
            nextWanderTime = Time.time + 5f;
            StateChanged?.Invoke(this);
            return true;
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            if (following) return pen != null && pen.Contains(transform.position, 0.6f)
                ? "Cow: E release into pen" : "Cow: E release lead (bring to pen)";
            if (!penned) return "Cow: E attach lead";
            if (CanMilk(DateTime.UtcNow)) return "Cow: E milk (+1 milk)";
            if (CanFeed) return FarmProgression.Instance != null && FarmProgression.Instance.CarrotCount < 1
                ? "Cow: needs 1 carrot to feed" : "Cow: E feed (1 carrot)";
            TimeSpan remaining = new TimeSpan(Math.Max(0, milkReadyUtcTicks - DateTime.UtcNow.Ticks));
            return "Cow: milk in " + Mathf.CeilToInt((float)remaining.TotalSeconds) + "s";
        }

        public bool Feed(DateTime now)
        {
            if (!CanFeed) return false;
            milkReadyUtcTicks = now.AddSeconds(MilkDelaySeconds).Ticks;
            eatingUntil = Time.time + 2.5f;
            StateChanged?.Invoke(this);
            return true;
        }

        public bool CanMilk(DateTime now) => penned && milkReadyUtcTicks > 0 && now.Ticks >= milkReadyUtcTicks;

        public bool Milk(DateTime now)
        {
            if (!CanMilk(now)) return false;
            milkReadyUtcTicks = 0;
            StateChanged?.Invoke(this);
            return true;
        }

        private void StopFollowing()
        {
            following = false;
            if (lead != null) lead.enabled = false;
        }

        public CowSaveData Snapshot() => new CowSaveData
        {
            id = cowId,
            penned = penned,
            x = transform.position.x,
            z = transform.position.z,
            milkReadyUtcTicks = milkReadyUtcTicks
        };

        public void Restore(CowSaveData saved)
        {
            if (saved == null) return;
            penned = saved.penned;
            following = false;
            milkReadyUtcTicks = Math.Max(0, saved.milkReadyUtcTicks);
            Vector3 restored = new Vector3(saved.x, transform.position.y, saved.z);
            if (penned && pen != null) restored = pen.ClampInside(restored, 1.2f);
            if (valleyGround != null && valleyGround.Raycast(new Ray(restored + Vector3.up * 12f, Vector3.down), out RaycastHit hit, 30f))
                restored.y = hit.point.y;
            transform.position = restored;
            home = penned ? transform.position : home;
            wanderTarget = transform.position;
            nextWanderTime = Time.time + 5f;
            if (lead != null) lead.enabled = false;
        }
    }
}
