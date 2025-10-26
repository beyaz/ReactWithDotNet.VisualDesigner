using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ReactWithDotNet.VisualDesigner;

public static partial class Extensions
{
    public static bool HasMatch<T>(this T value, Predicate<T> matchFunc) where T : class
    {
        if (value is null)
        {
            return false;
        }

        return matchFunc(value);
    }
    
    
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> source)
        => source as IReadOnlyList<T> ?? source.ToList();
    
    public static T FirstOrDefaultOf<T>(IEnumerable<T> items) => items.FirstOrDefault();
    
    public static T FirstOf<T>(IEnumerable<T> items) => items.First();
    
    public static bool HasAny<T>(IEnumerable<T> items) => items.Any();
    
    public static string KebabToCamelCase(string kebab)
    {
        if (string.IsNullOrEmpty(kebab))
        {
            return kebab;
        }

        var camelCase = new StringBuilder();
        var capitalizeNext = false;

        foreach (var c in kebab)
        {
            if (c == '-')
            {
                capitalizeNext = true;
            }
            else
            {
                if (capitalizeNext)
                {
                    camelCase.Append(char.ToUpper(c, CultureInfo.InvariantCulture));
                    capitalizeNext = false;
                }
                else
                {
                    camelCase.Append(c);
                }
            }
        }

        return camelCase.ToString();
    }
    
    public static string EnvironmentUserName => Environment.UserName;
    
    public static bool IsEqualsIgnoreWhitespace(string a, string b)
    {
        return ignore_whitespace_characters(a) == ignore_whitespace_characters(b);
        
        static string ignore_whitespace_characters(string value)
        {
            if (value == null)
            {
                return null;
            }

            return Regex.Replace(value, @"\s+", string.Empty);
        }
    }
    
    public static bool IsAlphaNumeric(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        return input.All(char.IsLetterOrDigit);
    }
    
    
    
    public static Result<T> Try<T>(Func<T> func)
    {
        try
        {
            return func();
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
    public static Maybe<int> TryParseInt32(string text)
    {
        if (int.TryParse(text, out var number))
        {
            return number;
        }

        return None;
    }
    
    public static Maybe<double> TryParseDouble(string text)
    {
        if (double.TryParse(text, out var number))
        {
            return number;
        }

        return None;
    }
    
    public static readonly CachedObjectMap Cache = new() { Timeout = TimeSpan.FromMinutes(5) };

    public static readonly CultureInfo CultureInfo_en_US = new("en-US");

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
        "base",
        "#text"
    ];

    public static string AsPixel(this double value)
    {
        return value.ToString(CultureInfo_en_US) + "px";
    }

    

    public static double CalculateTextWidth(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 50;
        }

        var textLength = text.Length;

        if (textLength == 1)
        {
            textLength = 2;
        }

        return textLength * 7.8;
    }

    public static string ClearConnectedValue(string value)
    {
        return value.RemoveFromStart("{").RemoveFromEnd("}");
    }

    public static T CloneByUsingYaml<T>(T value) where T : class
    {
        if (value is null)
        {
            return null;
        }

        return DeserializeFromYaml<T>(SerializeToYaml(value));
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

    public static Element EditorFontLinks(ReactContext context)
    {
        return new Fragment
        {
            new style
            {
                $$"""
                  
                  @font-face {
                    font-family: 'Wix Madefor Text';
                    src: url('{{context.wwwroot}}/fonts/WixMadeforText-Regular.woff2') format('woff2');
                    font-weight: normal;
                    font-style: normal;
                    font-display: swap;
                  }
                                  
                  """
            }
        };
    }

   

    
    public static bool HasNoValue(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
   
    public static async Task<Result<(string filePath, string targetComponentName)>> GetComponentFileLocation(int componentId, string userLocalWorkspacePath)
    {
        if (userLocalWorkspacePath.HasNoValue())
        {
            return new ArgumentNullException(nameof(userLocalWorkspacePath));
        }
        
        var component = await Store.TryGetComponent(componentId);

        var exportFilePath = component.GetExportFilePath();

        exportFilePath = Plugin.AnalyzeExportFilePath(exportFilePath);

        var filePath = Path.Combine(userLocalWorkspacePath, Path.Combine(exportFilePath.Split('/', Path.DirectorySeparatorChar)));
        
        return (filePath, component.GetName());
    }

    

    

    public static bool HasValue(this string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    public static bool In<T>(this T item, params T[] list)
    {
        return list.Contains(item);
    }

    public static bool IsConnectedValue(string value)
    {
        if (value is null)
        {
            return false;
        }

        return value.StartsWith("{") && value.EndsWith("}");
    }
    
    public static bool IsJsonArray(string value)
    {
        if (value is null)
        {
            return false;
        }

        return value.StartsWith("[") && value.EndsWith("]");
    }

    
    
    public static bool IsDouble(this string value)
    {
        return double.TryParse(value, CultureInfo_en_US, out _);
    }

    public static bool IsRawStringValue(string value)
    {
        if (value is null)
        {
            return false;
        }

        return value.TrimStart().StartsWith("| ");
    }

    public static bool IsStringTemplate(string value)
    {
        if (value is null)
        {
            return false;
        }

        return value.StartsWith("`") && value.EndsWith("`");
    }

    public static bool IsStringValue(string value)
    {
        if (value is null)
        {
            return false;
        }

        return (value.StartsWith("'") && value.EndsWith("'")) || (value.StartsWith("\"") && value.EndsWith("\""));
    }

    public static bool IsTrue(this Maybe<bool> maybe)
    {
        return maybe.HasValue && maybe.Value;
    }

    public static List<T> ListFrom<T>(IEnumerable<T> enumerable)
    {
        return enumerable.ToList();
    }
    
    public static Result<List<T>> ListFrom<T>(IEnumerable<Result<T>> enumerable)
    {
        var items = new List<T>();

        foreach (var item in enumerable)
        {
            if (item.HasError)
            {
                return item.Error;
            }

            items.Add(item.Value);
        }

        return items;
    }

    public static IReadOnlyDictionary<string, string> MapFrom((string key, string value)[] items)
    {
        return items.ToDictionary(x => x.key, x => x.value);
    }

    public static VisualElementModel Modify(VisualElementModel root, VisualElementModel target, Func<VisualElementModel, VisualElementModel> modifyNode)
    {
        if (root == target)
        {
            return modifyNode(root);
        }

        return root with
        {
            Children = ListFrom(from child in root.Children select Modify(child, target, modifyNode))
        };
    }
    
    public static VisualElementModel ModifyElements(VisualElementModel root, Func<VisualElementModel, bool> match, Func<VisualElementModel, VisualElementModel> modify)
    {
        // Eğer kök eleman match fonksiyonuna uyuyorsa modify et
        var modifiedRoot = match(root) ? modify(root) : root;

        // Çocuk elemanları yine bu fonksiyonla recursive olarak işleyin
        var modifiedChildren = modifiedRoot.Children
                                           .Select(child => ModifyElements(child, match, modify))
                                           .ToList();

        // Yeni bir VisualElementModel oluştur ve değiştirilen çocukları ekle
        return modifiedRoot with { Children = modifiedChildren };
    }

    /// <summary>
    ///     Bir öğeyi, listede başka bir öğenin önüne veya arkasına taşır.
    ///     Drag-and-drop gibi işlemler için idealdir.
    /// </summary>
    public static IReadOnlyList<T> MoveItemRelativeTo<T>(this IReadOnlyList<T> list, int sourceIndex, int targetIndex, bool insertBefore)
    {
        if (list == null || sourceIndex == targetIndex || sourceIndex < 0 || targetIndex < 0 ||
            sourceIndex >= list.Count || targetIndex >= list.Count)
        {
            return list;
        }

        var item = list[sourceIndex];

        list = list.RemoveAt(sourceIndex);

        if (sourceIndex < targetIndex)
        {
            targetIndex--;
        }

        var insertIndex = insertBefore ? targetIndex : targetIndex + 1;

        if (insertIndex > list.Count)
        {
            insertIndex = list.Count;
        }

        if (insertIndex < 0)
        {
            insertIndex = 0;
        }

        return list.Insert(insertIndex, item);
    }

    public static bool NotIn<T>(this T item, params T[] list)
    {
        return !list.Contains(item);
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

    public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, params TKey[] keys)
    {
        foreach (var key in keys)
        {
            dictionary.Remove(key);
        }
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

    public static void Then<A, B>(this (A a, B b) tuple, Action<A, B> nextAction)
    {
        nextAction(tuple.a, tuple.b);
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

    public static string TryClearRawStringValue(string value)
    {
        return value?.TrimStart().RemoveFromStart("| ");
    }

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

    public static Maybe<Type> TryGetHtmlElementTypeByTagName(string tag)
    {
        return typeof(svg).Assembly.GetType(nameof(ReactWithDotNet) + "." + tag, false, false);
    }

    public static Maybe<string> TryGetPropertyValue(this IReadOnlyList<string> properties, params string[] propertyNameWithAlias)
    {
        foreach (var property in properties)
        {
            foreach (var parsedProperty in TryParseProperty(property))
            {
                foreach (var propertyName in propertyNameWithAlias)
                {
                    if (parsedProperty.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return parsedProperty.Value;
                    }
                }
            }
        }

        return None;
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

   
    public static Maybe<int> TryReadTagAsDesignerComponentId(VisualElementModel model)
    {
        if (int.TryParse(model.Tag, out var componentId))
        {
            return componentId;
        }

        return None;
    }
}