using System.IO;
using System.Globalization;

namespace ReactWithDotNet.VisualDesigner;

static class Extensions
{
    public static bool IsDouble(this string input)
    {
        return double.TryParse(input, out _);
    }

    public static List<T> ListFrom<T>(IEnumerable<T> enumerable) => enumerable.ToList();

    public static IReadOnlyDictionary<string, string> MapFrom((string key, string value)[] items)
    {
        return items.ToDictionary(x => x.key, x => x.value);
    }
    
    //public static VisualElementModel Fix(this VisualElementModel model)
    //{
    //    var bindPropertyIndex = -1;
        
    //    string bindValue = null;
        
    //    for (var i = 0; i < model.Properties.Count; i++)
    //    {
    //        var result = TryParsePropertyValue(model.Properties[i]);
    //        if (result.HasValue)
    //        {
    //            var name = result.Name;
    //            var value = result.Value;

    //            if (name == "-bind")
    //            {
    //                bindPropertyIndex = i;

    //                bindValue = value;
                    
    //                if (model.Text.HasNoValue())
    //                {
    //                    throw new ArgumentException("Text cannot be null");
    //                }
    //            }
    //        }
    //    }

    //    if (bindPropertyIndex >= 0)
    //    {
    //        model.Properties.RemoveAt(bindPropertyIndex);
            
    //        model.Properties.Insert(0,$"-text: {ClearConnectedValue(bindValue)}");
    //        model.Properties.Insert(1,$"--text: '{TryClearStringValue(model.Text)}'");

    //        model.Text = null;
    //    }
    //    else if (model.Text.HasValue())
    //    {
    //        model.Properties.Insert(0, $"-text: '{TryClearStringValue(model.Text)}'");

    //        model.Text = null;
    //    }

    //    foreach (var child in model.Children)
    //    {
    //        Fix(child);
    //    }

    //    return model;
    //}
    
    
    public static bool HasNoText(this VisualElementModel model)
    {
        return GetText(model).HasNoValue();
    }
    public static bool HasText(this VisualElementModel model)
    {
        return GetText(model).HasValue();
    }
    
    public static string GetText(this VisualElementModel model)
    {
        var query = 
            from p in model.Properties
            from v in TryParseProperty(p)
            where v.Name == Design.Text
            select v.Value;

        return query.FirstOrDefault();

    }

    public static string GetDesignText(this VisualElementModel model)
    {
        var query = 
            from p in model.Properties
            from v in TryParseProperty(p)
            where v.Name == Design.DesignText
            select v.Value;

        return query.FirstOrDefault();
    }


    public static Maybe<Type> TryGetHtmlElementTypeByTagName(string tag)
    {
        return typeof(svg).Assembly.GetType(nameof(ReactWithDotNet) + "." + tag, false);
    }
    
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
    
    public static T CloneByUsingYaml<T>(T value) where T : class
    {
        if (value is null)
        {
            return null;
        }
        
        return DeserializeFromYaml<T>(SerializeToYaml(value));
    }
    
    public static VisualElementModel AsVisualElementModel(this string rootElementAsYaml)
    {
        if (rootElementAsYaml is null)
        {
            return null;
        }

        var value = DeserializeFromYaml<VisualElementModel>(rootElementAsYaml);
        

        return value;

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
    public static bool IsStringTemplate(string value)
    {
        if (value is null)
        {
            return false;
        }

        return value.StartsWith("`") && value.EndsWith("`");
    }
    
    public static string ClearConnectedValue(string value) => value.RemoveFromStart("{").RemoveFromEnd("}");
    
    public static string TryClearStringValue(string value)
    {
        if (value is null)
        {
            return null;
        }

        if (IsStringValue(value))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }
    
    
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
    
    public static Maybe<(string Name, string Value)> TryParseProperty(string nameValueCombined)
    {
        if (string.IsNullOrWhiteSpace(nameValueCombined))
        {
            return None;
        }

        if (nameValueCombined.StartsWith("..."))
        {
            return (
        
                Name     : Design.SpreadOperator,
                Value    : nameValueCombined
            );
        }
        
        var colonIndex = nameValueCombined.IndexOf(':');
        if (colonIndex < 0)
        {
            return None;
        }

        var name = nameValueCombined[..colonIndex];

        var value = nameValueCombined[(colonIndex + 1)..].Trim();
        if (value == string.Empty)
        {
            value = null;
        }

        return (
        
            Name     : name.Trim(),
            Value    : value
        );
    }
    
    public static bool In<T>(this T item, params T[] list)
    {
        return list.Contains(item);
    }
    
    public static bool IsTrue(this Maybe<bool> maybe)
    {
        return maybe.HasValue && maybe.Value;
    }

    public static bool NotIn<T>(this T item, params T[] list)
    {
        return !list.Contains(item);
    }
    
    public static Maybe<string> TryGetPropertyValue(this IReadOnlyList<string> properties, params string[] propertyNameWithAlias)
    {
        foreach (var property in properties)
        {
            var parseResult = TryParseProperty(property);
            if (parseResult.HasValue)
            {
                var name = parseResult.Value.Name;
                var value = parseResult.Value.Value;
                
                foreach (var propertyName in propertyNameWithAlias)
                {
                    if (name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) )
                    {
                        return value;
                    }
                }
            }
        }

        return None;
    }
}

public sealed class StyleAttribute
{
    public string Name { get; init; }
    public string Value { get; init; }
    public string Pseudo { get; init; }
    
    public static implicit operator (string name, string value, string pseudo)(StyleAttribute item)
    {
        return (item.Name, item.Value, item.Pseudo);
    }

    public void Deconstruct(out string name , out string value, out string pseudo)
    {
        name = Name;
        value = Value;
        pseudo = Pseudo;
    }
}

static class Design
{
    public const string Text = "-text";
    
    public const string DesignText = "--text";
    
    public const string SpreadOperator = "--spreadOperator";
}