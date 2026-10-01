using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmRestoration
{
    public sealed class HotbarToolButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private PlayerToolController controller;
        [SerializeField] private int shortcutIndex;

        public void Configure(PlayerToolController target, int index)
        {
            controller = target;
            shortcutIndex = index;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (controller != null) controller.SelectTool(shortcutIndex);
        }
    }
}
