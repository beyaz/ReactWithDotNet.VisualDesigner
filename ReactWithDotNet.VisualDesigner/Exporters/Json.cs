using Newtonsoft.Json;

namespace ReactWithDotNet.VisualDesigner.Exporters;

static class Json
{
    static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };
    
    public static string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, Settings);
    }
    
    public static T Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }
}