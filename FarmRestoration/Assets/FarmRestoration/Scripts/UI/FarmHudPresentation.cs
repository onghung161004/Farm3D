namespace FarmRestoration
{
    public static class FarmHudPresentation
    {
        public static string ToolText(FarmTool tool)
        {
            return "Tool: " + GetToolName(tool);
        }

        public static string PromptText(string prompt)
        {
            return "Action: " + (string.IsNullOrEmpty(prompt) ? "Move closer to a farm plot" : prompt);
        }

        public static string CropText(int cropCount)
        {
            return "Crops: " + (cropCount < 0 ? 0 : cropCount);
        }

        private static string GetToolName(FarmTool tool)
        {
            switch (tool)
            {
                case FarmTool.Hoe:
                    return "Hoe";
                case FarmTool.Seeds:
                    return "Seeds";
                case FarmTool.WateringCan:
                    return "Watering Can";
                case FarmTool.Harvest:
                    return "Harvest";
                default:
                    return tool.ToString();
            }
        }
    }
}
