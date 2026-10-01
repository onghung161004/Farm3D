using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    public static class InteractionTargetResolver
    {
        public static bool TryGetInteractable(IList<MonoBehaviour> components, out IInteractable target)
        {
            for (int index = 0; index < components.Count; index++)
            {
                IInteractable interactable = components[index] as IInteractable;
                if (interactable != null)
                {
                    target = interactable;
                    return true;
                }
            }

            target = null;
            return false;
        }
    }
}
