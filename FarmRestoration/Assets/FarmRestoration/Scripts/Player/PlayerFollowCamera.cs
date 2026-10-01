using UnityEngine;

namespace FarmRestoration
{
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 6.5f, -7.5f);
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.16f;
        [SerializeField, Min(0f)] private float rotationSharpness = 12f;

        private Vector3 positionVelocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                target.position + offset,
                ref positionVelocity,
                positionSmoothTime);

            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
        }

        public void ConfigureTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
