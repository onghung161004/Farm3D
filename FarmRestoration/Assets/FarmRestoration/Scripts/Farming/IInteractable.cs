namespace FarmRestoration
{
    public interface IInteractable
    {
        bool TryInteract(FarmTool tool);

        string GetInteractionPrompt(FarmTool tool);
    }
}
