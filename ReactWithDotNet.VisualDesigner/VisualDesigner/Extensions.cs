using System.IO;
using Newtonsoft.Json;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

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
    
    public static void Replace<T>(this List<T> list, int indexA, int indexB)
    {
        if (indexA < 0 || indexB < 0 || indexA >= list.Count || indexB >= list.Count)
            throw new ArgumentOutOfRangeException("Index out of range");

        (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
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
            node = node.Children[index];
        }

        return node;
    }
    
    
    
    public static IReadOnlyList<PropertyInfo> StyleProperties = new List<PropertyInfo>
    {
        new()
        {
            Name        = "width",
            Suggestions = ["auto", "fit-content", "max-content", "min-content", "inherit", "initial", "unset", "100%", "75%", "50%", "25%"]
        },
        new()
        {
            Name        = "height",
            Suggestions = ["auto", "fit-content", "max-content", "min-content", "inherit", "initial", "unset", "100%", "50px", "calc(100vh - 10px)"]
        },
        new()
        {
            Name        = "max-width",
            Suggestions = ["none", "fit-content", "max-content", "min-content", "inherit", "initial", "unset", "100%", "500px"]
        },
        new()
        {
            Name        = "max-height",
            Suggestions = ["none", "fit-content", "max-content", "min-content", "inherit", "initial", "unset", "100vh", "400px"]
        },
        new()
        {
            Name        = "min-width",
            Suggestions = ["0", "fit-content", "max-content", "min-content", "inherit", "initial", "unset", "200px"]
        },
        new()
        {
            Name        = "min-height",
            Suggestions = ["0", "fit-content", "max-content", "min-content", "inherit", "initial", "unset", "150px"]
        },
        new()
        {
            Name        = "display",
            Suggestions = ["block", "inline", "inline-block", "flex", "grid", "none", "contents", "table", "table-row", "table-cell", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "position",
            Suggestions = ["static", "relative", "absolute", "fixed", "sticky", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "top",
            Suggestions = ["auto", "inherit", "initial", "unset", "50px", "10%"]
        },
        new()
        {
            Name        = "bottom",
            Suggestions = ["auto", "inherit", "initial", "unset", "20px", "5%"]
        },
        new()
        {
            Name        = "left",
            Suggestions = ["auto", "inherit", "initial", "unset", "30px", "10%"]
        },
        new()
        {
            Name        = "right",
            Suggestions = ["auto", "inherit", "initial", "unset", "40px", "15%"]
        },
        new()
        {
            Name        = "z-index",
            Suggestions = ["auto", "inherit", "initial", "unset", "10", "1000"]
        },
        new()
        {
            Name        = "flex-direction",
            Suggestions = ["row", "row-reverse", "column", "column-reverse", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "justify-content",
            Suggestions = ["flex-start", "flex-end", "center", "space-between", "space-around", "space-evenly", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "align-items",
            Suggestions = ["stretch", "flex-start", "flex-end", "center", "baseline", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "gap",
            Suggestions = ["normal", "inherit", "initial", "unset", "10px", "1rem"]
        },
        new()
        {
            Name        = "overflow",
            Suggestions = ["visible", "hidden", "scroll", "auto", "clip", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "visibility",
            Suggestions = ["visible", "hidden", "collapse", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "opacity",
            Suggestions = ["0", "0.1", "0.5", "1", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "cursor",
            Suggestions = ["auto", "default", "pointer", "wait", "text", "move", "not-allowed", "crosshair", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "background",
            Suggestions = ["none", "inherit", "initial", "unset", "red", "url('image.jpg')", "linear-gradient(to right, red, blue)"]
        },
        new()
        {
            Name        = "border",
            Suggestions = ["none", "inherit", "initial", "unset", "1px solid black", "2px dashed red"]
        },
        new()
        {
            Name        = "border-radius",
            Suggestions = ["0", "inherit", "initial", "unset", "10px", "50%"]
        },
        new()
        {
            Name        = "box-shadow",
            Suggestions = ["none", "inherit", "initial", "unset", "2px 2px 5px gray"]
        },
        new()
        {
            Name        = "color",
            Suggestions = ["inherit", "initial", "unset", "black", "red", "blue", "#ff0000"]
        },
        new()
        {
            Name        = "font-size",
            Suggestions = ["small", "medium", "large", "inherit", "initial", "unset", "16px", "1.2rem"]
        },
        new()
        {
            Name        = "font-weight",
            Suggestions = ["normal", "bold", "bolder", "lighter", "100", "200", "300", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "line-height",
            Suggestions = ["normal", "inherit", "initial", "unset", "1.5", "2"]
        },
        new()
        {
            Name        = "text-align",
            Suggestions = ["left", "right", "center", "justify", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "text-decoration",
            Suggestions = ["none", "underline", "overline", "line-through", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "white-space",
            Suggestions = ["normal", "nowrap", "pre", "pre-wrap", "pre-line", "inherit", "initial", "unset"]
        },
        new()
        {
            Name        = "pointer-events",
            Suggestions = ["auto", "none", "inherit", "initial", "unset"]
        }
    };

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
            FontSize(13),
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
    
    public static (bool success, string name, string value) TryParsePropertyValue(string nameValueCombined)
    {
        if (string.IsNullOrWhiteSpace(nameValueCombined))
        {
            return default;
        }

        var colonIndex = nameValueCombined.IndexOf(':');
        if (colonIndex < 0)
        {
            return default;
        }

        var name = nameValueCombined[..colonIndex];

        var value = nameValueCombined[(colonIndex + 1)..];

        return (success: true, name.Trim(), value.Trim());
    }
    
    
    public static string TryGetPropertyValue(this IReadOnlyList<string> properties, params string[] propertyNameWithAlias)
    {
        foreach (var property in properties)
        {
            var (success, name, value) = TryParsePropertyValue(property);
            if (success)
            {
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