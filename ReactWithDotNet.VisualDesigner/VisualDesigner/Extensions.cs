using System.Globalization;
using System.IO;

namespace ReactWithDotNet.VisualDesigner;

static class Extensions
{
    
    
    public class IO
    {
        public static async Task<Result<string>> TryReadFile(string filePath)
        {
            try
            {
                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        public static async Task<Result<string[]>> TryReadFileAllLines(string filePath)
        {
            try
            {
                return await File.ReadAllLinesAsync(filePath);
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        public static async Task<Result> TryWriteToFile(string filePath, string fileContent)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, fileContent);
            }
            catch (Exception exception)
            {
                return exception;
            }

            return Success;
        }
    }
    public static Result<int> GetComponentDeclerationLineIndex(IReadOnlyList<string> fileContent, string targetComponentName)
    {
        var lines = fileContent.ToList();
        
        var componentDeclerationLineIndex = lines.FindIndex(line => line.Contains($"function {targetComponentName}("));
        if (componentDeclerationLineIndex == -1)
        {
            componentDeclerationLineIndex = lines.FindIndex(line => line.Contains($"const {targetComponentName} "));
            if (componentDeclerationLineIndex == -1)
            {
                componentDeclerationLineIndex = lines.FindIndex(line => line.Contains($"const {targetComponentName}:"));
                if (componentDeclerationLineIndex == -1)
                {
                    return new ArgumentException($"ComponentDeclerationNotFoundInFile. {targetComponentName}");
                }
            }
        }

        return componentDeclerationLineIndex;
    }
    
    public static Result<(string filePath, string targetComponentName)> GetComponentFileLocation(int componentId, string userLocalWorkspacePath)
    {
        // todo: await
        var component = DbOperation(db => db.FirstOrDefault<ComponentEntity>(x => x.Id == componentId));

        return (Path.Combine(userLocalWorkspacePath, Path.Combine(component.GetExportFilePath().Split(new[] { '/', Path.DirectorySeparatorChar }))), component.GetName());
    }
    
    public static Maybe<(string width, string style, string color)> TryParseBorderCss(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return None;
        }
                        
        List<string> borderStyles = ["solid", "dashed", "dotted", "double", "none"];
                        
        var width = parts[0];
                        
        foreach (var borderStyle in borderStyles)
        {
            if (parts[1] == borderStyle)
            {
                return (width, borderStyle, parts[2]);
            }
                            
            if (parts[2] == borderStyle)
            {
                return (width, borderStyle, parts[1]);
            }
        }
                        
        return None;
    }
    
    public   static Maybe<int> TryReadTagAsDesignerComponentId(VisualElementModel model)
    {
        if (int.TryParse(model.Tag, out var componentId))
        {
            return componentId;
        }

        return None;
    }

    public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, params TKey[] keys)
    {
        foreach (var key in keys)
        {
            dictionary.Remove(key);
        }
    }
    
    /// <summary>
    /// Bir öğeyi, listede başka bir öğenin önüne veya arkasına taşır.
    /// Drag-and-drop gibi işlemler için idealdir.
    /// </summary>
    public static void MoveItemRelativeTo<T>(this List<T> list, int sourceIndex, int targetIndex, bool insertBefore)
    {
        if (list == null || sourceIndex == targetIndex || sourceIndex < 0 || targetIndex < 0 ||
            sourceIndex >= list.Count || targetIndex >= list.Count)
        {
            return;
        }

        var item = list[sourceIndex];
        
        list.RemoveAt(sourceIndex);

        if (sourceIndex < targetIndex)
            targetIndex--;

        int insertIndex = insertBefore ? targetIndex : targetIndex + 1;

        if (insertIndex > list.Count)
        {
            insertIndex = list.Count;
        }

        if (insertIndex < 0)
        {
            insertIndex          = 0;
        }

        list.Insert(insertIndex, item);
    }
    
    

    public static List<T> ListFrom<T>(IEnumerable<T> enumerable) => enumerable.ToList();

    public static IReadOnlyDictionary<string, string> MapFrom((string key, string value)[] items)
    {
        return items.ToDictionary(x => x.key, x => x.value);
    }
    
    public static void Then<A, B>(this (A a, B b) tuple, Action<A, B> nextAction)
    {
        nextAction(tuple.a, tuple.b);
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
    
    public static bool IsDouble(this string value)
    {
        return double.TryParse(value, CultureInfo_en_US, out _);
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