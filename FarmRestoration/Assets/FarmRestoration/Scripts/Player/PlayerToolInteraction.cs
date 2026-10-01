namespace FarmRestoration
{
    public static class PlayerToolInteraction
    {
        public static bool TryUse(IInteractable target, FarmTool tool)
        {
            return target != null && target.TryInteract(tool);
        }
    }
}
