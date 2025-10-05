using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Toolbox;

static class YamlHelper
{
    public static T DeserializeFromYaml<T>(string yamlContent) where T: class
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
            .Build();

        return deserializer.Deserialize<T>(yamlContent);
    }
    
    public static string SerializeToYaml<T>(T obj)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
            .Build();

        return serializer.Serialize(obj);
    }

    sealed class ReadOnlyCollectionNodeTypeResolver : INodeTypeResolver
    {
        static readonly IReadOnlyDictionary<Type, Type> CustomGenericInterfaceImplementations = new Dictionary<Type, Type>
        {
            { typeof(IReadOnlyCollection<>), typeof(List<>) },
            { typeof(IReadOnlyList<>), typeof(List<>) },
            { typeof(IReadOnlyDictionary<,>), typeof(Dictionary<,>) }
        };

        public bool Resolve(NodeEvent nodeEvent, ref Type type)
        {
            if (type is { IsInterface: true, IsGenericType: true } && CustomGenericInterfaceImplementations.TryGetValue(type.GetGenericTypeDefinition(), out var concreteType))
            {
                type = concreteType.MakeGenericType(type.GetGenericArguments());
                return true;
            }

            return false;
        }
    }
}