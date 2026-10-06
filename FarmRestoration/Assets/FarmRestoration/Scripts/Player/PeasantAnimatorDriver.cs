using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class PeasantAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Animator animator;

        private bool wasMoving;
        private static readonly int WalkState = Animator.StringToHash("Base Layer.metarig|Walk");
        private static readonly int IdleState = Animator.StringToHash("Base Layer.metarig|Idle");

        public void Configure(PlayerMovement movement, Animator characterAnimator)
        {
            playerMovement = movement;
            animator = characterAnimator;
            if (animator != null) animator.applyRootMotion = false;
        }

        private void Awake()
        {
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
            if (animator != null) animator.applyRootMotion = false;
        }

        private void Update()
        {
            if (playerMovement == null || animator == null) return;
            bool moving = playerMovement.IsMoving;
            if (wasMoving == moving) return;
            wasMoving = moving;
            animator.CrossFade(moving ? WalkState : IdleState, 0.18f);
        }
    }
}
