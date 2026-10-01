namespace FarmRestoration
{
    public static class PlayerToolSelection
    {
        public static bool TryGetTool(int shortcutIndex, out FarmTool tool)
        {
            switch (shortcutIndex)
            {
                case 1:
                    tool = FarmTool.Hoe;
                    return true;
                case 2:
                    tool = FarmTool.Seeds;
                    return true;
                case 3:
                    tool = FarmTool.WateringCan;
                    return true;
                case 4:
                    tool = FarmTool.Harvest;
                    return true;
                default:
                    tool = FarmTool.Hoe;
                    return false;
            }
        }
    }
}
