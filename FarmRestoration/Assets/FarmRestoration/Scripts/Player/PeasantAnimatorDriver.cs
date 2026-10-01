using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class PeasantAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Animator animator;

        private bool wasMoving;

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
            if (playerMovement == null || animator == null || wasMoving == playerMovement.IsMoving) return;
            wasMoving = playerMovement.IsMoving;
            animator.CrossFade(wasMoving ? "Base Layer.metarig|Walk" : "Base Layer.metarig|Idle", 0.12f);
        }
    }
}
