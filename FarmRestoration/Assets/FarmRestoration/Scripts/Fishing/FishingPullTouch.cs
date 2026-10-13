using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmRestoration
{
    public sealed class FishingPullTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            if (FishingSession.Active != null) FishingSession.Active.SetPullHeld(true);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();
        private void OnDisable() => Release();

        private static void Release()
        {
            if (FishingSession.Active != null) FishingSession.Active.SetPullHeld(false);
        }
    }
}
