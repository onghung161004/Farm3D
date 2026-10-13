namespace FarmRestoration
{
    /// <summary>
    /// Centralizes the minimum readable HUD metrics used by the FarmDemo setup.
    /// Keeping these values framework-free makes the visual policy easy to test.
    /// </summary>
    public readonly struct FarmHudReadabilityStyle
    {
        public const float PanelWidth = 1040f;
        public const float PanelHeight = 460f;
        public const float LabelWidth = 980f;

        public float ToolFontSize => 52f;
        public float ActionFontSize => 36f;
        public float CropsFontSize => 34f;
        public float ControlsFontSize => 28f;

        public string ControlsText => "1 Hoe    2 Seeds    3 Water\n4 Harvest    E Interact";

        public static FarmHudReadabilityStyle Default => new FarmHudReadabilityStyle();
    }
}
