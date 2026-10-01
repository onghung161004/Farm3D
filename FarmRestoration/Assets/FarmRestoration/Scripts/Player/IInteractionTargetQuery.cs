using UnityEngine;

namespace FarmRestoration
{
    public interface IInteractionTargetQuery
    {
        bool TryFindTarget(Vector3 origin, float range, out IInteractable target);
    }
}
