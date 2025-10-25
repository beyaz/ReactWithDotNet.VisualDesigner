namespace ReactWithDotNet.VisualDesigner.DbModels;

static class Extensions
{
    public static IReadOnlyDictionary<string, string> GetConfig(this ComponentEntity componentEntity)
    {
        return DeserializeFromYaml<Dictionary<string, string>>(componentEntity.ConfigAsYaml);
    }
}