using System.IO;
using Newtonsoft.Json;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Formatting = Newtonsoft.Json.Formatting;
using System.Globalization;

namespace ReactWithDotNet.VisualDesigner;

static class Theme
{
    public const string BorderColor = "#d5d5d8";
    public static string BackgroundColor = "#eff3f8";
    public static string WindowBackgroundColor = rgba(255, 255, 255, 0.4);
    public static string text_primary => "#1A2027";
}

static class YamlHelper
{
    public static T DeserializeFromYaml<T>(string yamlContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithNodeTypeResolver(new ReadOnlyCollectionNodeTypeResolver())
            .Build();

        return deserializer.Deserialize<T>(yamlContent);
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
            if (type.IsInterface && type.IsGenericType && CustomGenericInterfaceImplementations.TryGetValue(type.GetGenericTypeDefinition(), out var concreteType))
            {
                type = concreteType.MakeGenericType(type.GetGenericArguments());
                return true;
            }

            return false;
        }
    }
}

static class Extensions
{
    public static readonly CultureInfo CultureInfo_en_US = new("en-US");
    public static string AsPixel(this double value)
    {
        return value.ToString(CultureInfo_en_US) + "px";
    }
    
    public static bool HasNoChild(this VisualElementModel model)
    {
        return model.Children is null || model.Children.Count == 0;
    }
    
    public static void RefreshComponentPreview(this Client client)
    {
        const string jsCode =
            """
            var frame = document.getElementById('ComponentPreview')
            if(frame)
            {
              var reactWithDotNet = frame.contentWindow.ReactWithDotNet;
              if(reactWithDotNet)
              {
                reactWithDotNet.DispatchEvent('RefreshComponentPreview', []);
              }
            }
            """;
        
        client.RunJavascript(jsCode);
    }
    
  
    
    public static IReadOnlyList<string> TagNameList =>
    [
        "html",
        "head",
        "title",
        "body",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "p",
        "hr",
        "br",
        "wbr",
        "strong",
        "b",
        "em",
        "i",
        "mark",
        "small",
        "del",
        "ins",
        "sub",
        "sup",
        "code",
        "pre",
        "blockquote",
        "cite",
        "q",
        "abbr",
        "dfn",
        "kbd",
        "samp",
        "var",
        "time",
        "a",
        "nav",
        "ul",
        "ol",
        "li",
        "dl",
        "dt",
        "dd",
        "table",
        "tr",
        "td",
        "th",
        "thead",
        "tbody",
        "tfoot",
        "caption",
        "colgroup",
        "col",
        "form",
        "input",
        "button",
        "textarea",
        "select",
        "option",
        "optgroup",
        "label",
        "fieldset",
        "legend",
        "datalist",
        "output",
        "meter",
        "progress",
        "img",
        "figure",
        "figcaption",
        "audio",
        "video",
        "source",
        "track",
        "canvas",
        "svg",
        "map",
        "area",
        "header",
        "footer",
        "main",
        "section",
        "article",
        "aside",
        "div",
        "span",
        "iframe",
        "embed",
        "object",
        "param",
        "script",
        "noscript",
        "template",
        "slot",
        "shadow",
        "style",
        "link",
        "meta",
        "base"

    ];
    
    public static VisualElementModel FindTreeNodeByTreePath(VisualElementModel node, string path)
    {
        if (path.HasNoValue())
        {
            return null;
        }
        
        foreach (var index in path.Split(',').Select(int.Parse).Skip(1))
        {
            if (node.Children.Count <= index)
            {
                return null;
            }
            
            node = node.Children[index];
        }

        return node;
    }
    
    public static string JsonPrettify(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }
        
        try
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }
        catch (Exception)
        {
           return json;
        }
    }

    public static string SerializeToJson(object obj)
    {
        if (obj is null)
        {
            return null;
        }
        
        return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
        {
            Formatting           = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Ignore
        });
    }
    
    public static T CloneByUsingJson<T>(T value) where T : class
    {
        if (value is null)
        {
            return null;
        }
        
        return DeserializeFromJson<T>(SerializeToJson(value));
    }
    
    public static T DeserializeFromJson<T>(string json) where T : class
    {
        if (json is null)
        {
            return null;
        }
        
        return JsonConvert.DeserializeObject<T>(json);
    }
    public static VisualElementModel AsVisualElementModel(this string rootElementAsJson)
    {
        if (rootElementAsJson is null)
        {
            return null;
        }

        var value = DeserializeFromJson<VisualElementModel>(rootElementAsJson);
        

        return value;

    }
    
    
    public static T DeserializeFromYaml<T>(string yamlContent) where T : class
    {
        if (yamlContent is null)
        {
            return null;
        }

        return YamlHelper.DeserializeFromYaml<T>(yamlContent);
    }
    
    /// <summary>
    ///     Removes value from end of str
    /// </summary>
    public static string RemoveFromEnd(this string data, string value)
    {
        return RemoveFromEnd(data, value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Removes from end.
    /// </summary>
    public static string RemoveFromEnd(this string data, string value, StringComparison comparison)
    {
        if (data.EndsWith(value, comparison))
        {
            return data.Substring(0, data.Length - value.Length);
        }

        return data;
    }

    /// <summary>
    ///     Removes value from start of str
    /// </summary>
    public static string RemoveFromStart(this string data, string value)
    {
        return RemoveFromStart(data, value, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsConnectedValue(string value)
    {
        if (value is null)
        {
            return false;
        }

        return value.StartsWith("{") && value.EndsWith("}");
    }
    
    public static bool IsStringValue(string value)
    {
        if (value is null)
        {
            return false;
        }

        return (value.StartsWith("'") && value.EndsWith("'")) || (value.StartsWith("\"") && value.EndsWith("\""));
    }
    
    public static string ClearConnectedValue(string value) => value.RemoveFromStart("{").RemoveFromEnd("}");
    
    /// <summary>
    ///     Removes value from start of str
    /// </summary>
    public static string RemoveFromStart(this string data, string value, StringComparison comparison)
    {
        if (data == null)
        {
            return null;
        }

        if (data.StartsWith(value, comparison))
        {
            return data.Substring(value.Length, data.Length - value.Length);
        }

        return data;
    }
    public static double CalculateTextWidth(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 50;
        }

        var textLegth = text.Length;

        if (textLegth == 1)
        {
            textLegth = 2;
        }

        return textLegth * 7.8;
    }

    public static Element EditorFontLinks()
    {
        return new Fragment
        {
            new link { href = "https://fonts.googleapis.com", rel = "preconnect" },

            new link { href = "https://fonts.gstatic.com", rel = "preconnect", crossOrigin = "true" },

            new link { href = "https://fonts.googleapis.com/css2?family=Wix+Madefor+Text:ital,wght@0,400..800;1,400..800&display=swap", rel = "stylesheet" },
            new link
            {
                href = "https://fonts.cdnfonts.com/css/ibm-plex-mono-3",
                rel  = "stylesheet"
            }
        };
    }
    
    public static StyleModifier[] EditorFont()
    {
        return
        [
            FontFamily("'Wix Madefor Text', sans-serif"),
            FontSize(12),
            LetterSpacingNormal
        ];
    }

    public static bool HasValue(this string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
    
    public static bool HasNoValue(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static string TryBeautifyPropertyValue(string nameValueCombined)
    {
        if (string.IsNullOrWhiteSpace(nameValueCombined))
        {
            return nameValueCombined;
        }

        var colonIndex = nameValueCombined.IndexOf(':');
        if (colonIndex < 0)
        {
            return nameValueCombined;
        }

        var name = nameValueCombined[..colonIndex];

        var value = nameValueCombined[(colonIndex + 1)..];

        return $"{name.Trim()}: {value.Trim()}";
    }
    
    public static AttributeParseResult TryParsePropertyValue(string nameValueCombined)
    {
        if (string.IsNullOrWhiteSpace(nameValueCombined))
        {
            return AttributeParseResult.Fail;
        }
        
        var colonIndex = nameValueCombined.IndexOf(':');
        if (colonIndex < 0)
        {
            return AttributeParseResult.Fail;
        }

        var name = nameValueCombined[..colonIndex];

        var value = nameValueCombined[(colonIndex + 1)..];

        return new()
        {
            success = true,
            name    = name.Trim(),
            value   = value.Trim(),
        };
    }
    
    public static StyleAttribute ParseStyleAttibute(string nameValueCombined)
    {
        if (string.IsNullOrWhiteSpace(nameValueCombined))
        {
            return null;
        }

        string pseudo = null;
        
        if (nameValueCombined.StartsWith("hover:"))
        {
            pseudo            = "hover";
            nameValueCombined = nameValueCombined.RemoveFromStart("hover:").Trim();
        }
        
        
        var colonIndex = nameValueCombined.IndexOf(':');
        if (colonIndex < 0)
        {
            return new()
            {
                name   = nameValueCombined.Trim(),
                Pseudo = pseudo
            };
        }

        var name = nameValueCombined[..colonIndex];

        var value = nameValueCombined[(colonIndex + 1)..];

        return new()
        {
            name    = name.Trim(),
            value   = value.Trim(),
            Pseudo  = pseudo
        };
    }
    
    
    public static string TryGetPropertyValue(this IReadOnlyList<string> properties, params string[] propertyNameWithAlias)
    {
        foreach (var property in properties)
        {
            var parseResult = TryParsePropertyValue(property);
            if (parseResult.success)
            {
                var name = parseResult.name;
                var value = parseResult.value;
                
                foreach (var propertyName in propertyNameWithAlias)
                {
                    if (name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) )
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

  
    
}


sealed class AttributeParseResult
{
    public bool success { get; init; }
    public string name { get; init; }
    public string value { get; init; }

    public static AttributeParseResult Fail=>new()
    {
        success = false
    };
}

sealed class StyleAttribute
{
    public string name { get; init; }
    public string value { get; init; }
    public string Pseudo { get; init; }
    
    public static implicit operator (string name, string value, string pseudo)(StyleAttribute item)
    {
        return (item.name, item.value, item.Pseudo);
    }

    public void Deconstruct(out string name , out string value, out string pseudo)
    {
        name = this.name;
        value = this.value;
        pseudo = this.Pseudo;

    }
}