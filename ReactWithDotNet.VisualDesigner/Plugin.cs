using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Mono.Cecil;
using ReactWithDotNet.VisualDesigner.Configuration;

namespace ReactWithDotNet.VisualDesigner;

using SuggestionItem = (string name, string value, JsType jsType, bool isVariable);

public class PluginComponentBase : Component
{
    public string id;
    public MouseEventHandler onMouseClick;
}

delegate Scope PluginMethod(Scope scope);

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomComponentAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ImportAttribute : Attribute
{
    public string Name { get; init; }

    public string Package { get; init; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class TryGetIconForElementTreeNodeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class IsImageAttribute : Attribute
{
}

public sealed record TryCreateElementForPreviewInput
{
    public string Id { get; init; }

    public MouseEventHandler OnMouseClick { get; init; }

    public string Tag { get; init; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class TryCreateElementForPreviewAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class AnalyzeExportFilePathAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterReadConfigAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class GetStringSuggestionsAttribute : Attribute
{
}

public sealed record PropSuggestionScope
{
    public ComponentEntity Component { get; init; }

    public Maybe<ComponentEntity> SelectedComponent { get; init; }

    public string TagName { get; init; }
}

public static class Plugin
{
    public static readonly ScopeKey<Element> CurrentElementInstanceInPreview
        = nameof(CurrentElementInstanceInPreview);

    public static readonly ScopeKey<string> ExportFilePathForComponent
        = nameof(ExportFilePathForComponent);

    public static readonly ScopeKey<Element> IconForElementTreeNode
        = nameof(IconForElementTreeNode);

    public static readonly ScopeKey<object> IsImageKey = nameof(IsImageKey);

    public static readonly ScopeKey<TryCreateElementForPreviewInput> TryCreateElementForPreviewInputKey
        = nameof(TryCreateElementForPreviewInputKey);

    public static readonly ScopeKey<Element> TryCreateElementForPreviewOutputKey
        = nameof(TryCreateElementForPreviewOutputKey);

    public static readonly ScopeKey<VisualElementModel> VisualElementModel
        = nameof(VisualElementModel);

    public static ScopeKey<ConfigModel> Config
        = nameof(Config);

    delegate Task<Result<IReadOnlyList<string>>> GetStringSuggestionsDelegate(PropSuggestionScope scope);

    public static IReadOnlyList<Type> AllCustomComponents
    {
        get
        {
            return field ??=
            (
                from assembly in Plugins
                from type in assembly.GetTypes()
                where type.GetCustomAttribute<CustomComponentAttribute>() is not null
                select type).AsReadOnlyList();
        }
    }

    public static IReadOnlyList<Assembly> Plugins
    {
        get
        {
            return field ??= new[] { typeof(Plugin).Assembly }.ToImmutableList().AddRange
            (
                from filePath in Directory.GetFiles(AppContext.BaseDirectory, "*.dll")
                where Path.GetFileName(filePath).StartsWith("ReactWithDotNet.VisualDesigner.Plugins.")
                select AssemblyLoadContext.Default.LoadFromAssemblyPath(filePath)
            );
        }
    }

    static IReadOnlyList<PluginMethod> AfterReadConfigs
    {
        get { return field ??= GetPluginMethods<AfterReadConfigAttribute>(); }
    }

    static IReadOnlyList<PluginMethod> AnalyzeExportFilePathList
    {
        get { return field ??= GetPluginMethods<AnalyzeExportFilePathAttribute>(); }
    }

    static IReadOnlyList<GetStringSuggestionsDelegate> GetStringSuggestionsMethods
    {
        get
        {
            return field ??= (
                    from assembly in Plugins
                    from type in assembly.GetTypes()
                    from methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    where methodInfo.GetCustomAttribute<GetStringSuggestionsAttribute>() is not null
                    select (GetStringSuggestionsDelegate)Delegate.CreateDelegate(typeof(GetStringSuggestionsDelegate), methodInfo))
               .ToList();
        }
    }

    static IReadOnlyList<PluginMethod> IsImageList
    {
        get { return field ??= GetPluginMethods<IsImageAttribute>(); }
    }

    static IReadOnlyList<PluginMethod> TryCreateElementForPreviewList
    {
        get { return field ??= GetPluginMethods<TryCreateElementForPreviewAttribute>(); }
    }

    static IReadOnlyList<PluginMethod> TryGetIconForElementTreeNodes
    {
        get { return field ??= GetPluginMethods<TryGetIconForElementTreeNodeAttribute>(); }
    }

    public static ConfigModel AfterReadConfig(ConfigModel config)
    {
        var scope = Scope.Create(new()
        {
            { Config, config }
        });

        return RunPluginMethods(AfterReadConfigs, scope, Config) ?? config;
    }

    public static string AnalyzeExportFilePath(string exportFilePathForComponent)
    {
        var scope = Scope.Create(new()
        {
            { ExportFilePathForComponent, exportFilePathForComponent }
        });

        return RunPluginMethods(AnalyzeExportFilePathList, scope, ExportFilePathForComponent) ?? exportFilePathForComponent;
    }

    public static Element BeforeComponentPreview(RenderPreviewScope scope, VisualElementModel visualElementModel, Element component)
    {
        return component;
    }

    public static string ConvertDotNetPathToJsPath(string dotNetPath)
    {
        if (string.IsNullOrEmpty(dotNetPath))
        {
            return dotNetPath;
        }

        var camelCase = new StringBuilder();
        var capitalizeNext = false;

        foreach (var c in dotNetPath)
        {
            if (c == '.')
            {
                capitalizeNext = true;
                camelCase.Append('.');
            }
            else
            {
                if (capitalizeNext)
                {
                    camelCase.Append(char.ToLower(c, CultureInfo.InvariantCulture));
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
    
  

    public static async Task<Result<IReadOnlyList<string>>> GetPropSuggestions(PropSuggestionScope scope)
    {
        if (scope.TagName.HasNoValue())
        {
            return Result.From((IReadOnlyList<string>)[]);
        }

        return await Cache.AccessValue($"{nameof(Plugin)}-{scope.TagName}", () => calculate(scope));

        static async Task<Result<IReadOnlyList<string>>> calculate(PropSuggestionScope scope)
        {
            List<SuggestionItem> suggestionItems = [];

            await foreach (var result in GetStringSuggestions(scope))
            {
                if (result.HasError)
                {
                    return result.Error;
                }

                suggestionItems.Add(new()
                {
                    jsType = JsType.String,
                    value  = result.Value
                });
            }

            suggestionItems.AddRange
            (
                from x in new[]
                {
                    "2",
                    "4",
                    "8",
                    "12",
                    "16",
                    "24"
                }
                select new SuggestionItem
                {
                    jsType = JsType.Number,

                    value = x
                }
            );

            suggestionItems.Add
            (
                new SuggestionItem
                {
                    jsType = JsType.Date,
                    value  = "new Date().getDate()"
                }
            );

            suggestionItems.AddRange
            (
                [
                    new SuggestionItem
                    {
                        jsType = JsType.Boolean,
                        value  = "true"
                    },
                    new SuggestionItem
                    {
                        jsType = JsType.Boolean,
                        value  = "false"
                    }
                ]
            );

            foreach (var variable in scope.Component.Config.DotNetVariables)
            {
                List<(JsType jsType, Func<PropertyDefinition, bool> matchFn)> map =
                [
                    (JsType.String, CecilHelper.IsString),
                    (JsType.Number, CecilHelper.IsNumber),
                    (JsType.Date, CecilHelper.IsDateTime),
                    (JsType.Boolean, CecilHelper.IsBoolean),
                    (JsType.Array, CecilHelper.IsCollection)
                ];

                foreach (var (jsType, fn) in map)
                {
                    var result = CecilHelper.GetPropertyPathList(variable.DotNetAssemblyFilePath, variable.DotnetTypeFullName, $"{variable.VariableName}.", fn);
                    if (result.HasError)
                    {
                        continue;
                    }

                    suggestionItems.AddRange
                    (
                        from x in result.Value
                        select new SuggestionItem
                        {
                            value = x,

                            jsType = jsType,

                            isVariable = true
                        }
                    );
                }
            }

            List<string> returnList = [];

            List<(string name, string value)> distinctSuggestions = [];

            suggestionItems.AddRange
            (
                from x in GetPropSuggestions(scope.TagName)
                select new SuggestionItem
                {
                    name   = x.name,
                    value  = x.value,
                    jsType = x.jsType
                }
            );

            var allMetadata = GetAllTypesMetadata().AsResult();
            if (allMetadata.HasError)
            {
                return allMetadata.Error;
            }

            IEnumerable<string> getNames(JsType jsType, params string[] extraNames)
            {
                return
                    ImmutableList<string>
                       .Empty.AddRange
                        (
                            from m in allMetadata.Value
                            where m.TagName == scope.TagName
                            from p in m.Props
                            where p.ValueType == jsType
                            select p.Name
                        )
                       .AddRange(extraNames);
            }

            // s t r i n g
            {
                const JsType jsType = JsType.String;

                foreach (var name in getNames(jsType, Design.Text))
                {
                    foreach (var x in from x in suggestionItems where x.jsType == jsType select x)
                    {
                        if (x.isVariable)
                        {
                            addSuggestion(name, ConvertDotNetPathToJsPath(x.value));
                        }
                        else
                        {
                            addSuggestion(name, '"' + x.value + '"');
                        }
                    }
                }
            }

            // number
            {
                const JsType jsType = JsType.Number;

                foreach (var name in getNames(jsType))
                {
                    foreach (var x in from x in suggestionItems where x.jsType == jsType select x)
                    {
                        if (x.isVariable)
                        {
                            addSuggestion(name, ConvertDotNetPathToJsPath(x.value));
                        }
                        else
                        {
                            addSuggestion(name, x.value);
                        }
                    }
                }
            }

            // Boolean
            {
                const JsType jsType = JsType.Boolean;

                foreach (var name in getNames(jsType, Design.ShowIf, Design.HideIf))
                {
                    foreach (var x in from x in suggestionItems where x.jsType == jsType select x)
                    {
                        if (x.isVariable)
                        {
                            addSuggestion(name, ConvertDotNetPathToJsPath(x.value));
                        }
                        else
                        {
                            addSuggestion(name, x.value);
                        }
                    }
                }
            }

            // Date
            {
                const JsType jsType = JsType.Date;

                foreach (var name in getNames(jsType))
                {
                    foreach (var x in from x in suggestionItems where x.jsType == jsType select x)
                    {
                        if (x.isVariable)
                        {
                            addSuggestion(name, ConvertDotNetPathToJsPath(x.value));
                        }
                        else
                        {
                            addSuggestion(name, x.value);
                        }
                    }
                }
            }

            // Array
            {
                const JsType jsType = JsType.Array;

                foreach (var name in getNames(jsType, Design.ItemsSource))
                {
                    foreach (var x in from x in suggestionItems where x.jsType == jsType select x)
                    {
                        if (x.isVariable)
                        {
                            addSuggestion(name, ConvertDotNetPathToJsPath(x.value));
                        }
                        else
                        {
                            addSuggestion(name, x.value);
                        }
                    }
                }
            }

            returnList.InsertRange(0, distinctSuggestions.Select(x => x.name + ": " + x.value));

            return returnList;

            void addSuggestion(string name, string value)
            {
                if (!distinctSuggestions.Any(x => name.Equals(x.name, StringComparison.OrdinalIgnoreCase)))
                {
                    distinctSuggestions.Add((name, value));
                    return;
                }

                returnList.Add($"{name}: {value}");
            }
        }
    }

    public static IEnumerable<(string name, string value, JsType jsType)> GetPropSuggestions(string tag)
    {
        var type = AllCustomComponents.FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            yield break;
        }

        foreach (var item in
                 from p in type.GetProperties()
                 from a in p.GetCustomAttributes<SuggestionsAttribute>()
                 from jsTypeInfo in p.GetCustomAttributes<JsTypeInfoAttribute>()
                 from suggestion in a.Suggestions
                 select (p.Name, suggestion, jsTypeInfo.JsType))
        {
            yield return item;
        }
    }

    public static IReadOnlyList<string> GetTagSuggestions()
    {
        return AllCustomComponents.Select(x => x.Name).ToList();
    }

    public static bool IsImage(Element element)
    {
        var scope = Scope.Create(new()
        {
            { CurrentElementInstanceInPreview, element }
        });

        return (bool?)RunPluginMethods(IsImageList, scope, IsImageKey) ?? false;
    }

    public static Element TryCreateElementForPreview(TryCreateElementForPreviewInput input)
    {
        var scope = Scope.Create(new()
        {
            { TryCreateElementForPreviewInputKey, input }
        });

        return RunPluginMethods(TryCreateElementForPreviewList, scope, TryCreateElementForPreviewOutputKey);
    }

    [TryCreateElementForPreview]
    public static Scope TryCreateElementForPreview(Scope scope)
    {
        var input = TryCreateElementForPreviewInputKey[scope];

        var type = AllCustomComponents.FirstOrDefault(t => t.Name.Equals(input.Tag, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            return Scope.Empty;
        }

        var component = (Element)Activator.CreateInstance(type);

        if (component is PluginComponentBase componentBase)
        {
            componentBase.id           = input.Id;
            componentBase.onMouseClick = input.OnMouseClick;
        }

        return Scope.Create(new()
        {
            { TryCreateElementForPreviewOutputKey, component }
        });
    }

    public static Element TryGetIconForElementTreeNode(VisualElementModel node)
    {
        var scope = Scope.Create(new()
        {
            { VisualElementModel, node }
        });

        return RunPluginMethods(TryGetIconForElementTreeNodes, scope, IconForElementTreeNode);
    }

    static IEnumerable<Result<ComponentMeta>> GetAllTypesMetadata()
    {
        return from type in AllCustomComponents select createFrom(type);

        static Result<ComponentMeta> createFrom(Type type)
        {
            var items = from propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        from props in createPropMetaFrom(propertyInfo)
                        select props;

            return
                from props in items.AsResult()
                select new ComponentMeta
                {
                    TagName = type.Name,
                    Props   = props.ToList()
                };

            static Result<PropMeta> createPropMetaFrom(PropertyInfo propertyInfo)
            {
                return
                    from valueType in getValueType(propertyInfo)
                    select new PropMeta
                    {
                        Name      = propertyInfo.Name,
                        ValueType = valueType
                    };

                static Result<JsType> getValueType(PropertyInfo propertyInfo)
                {
                    var jsTypeInfoAttribute = propertyInfo.GetCustomAttribute<JsTypeInfoAttribute>();
                    if (jsTypeInfoAttribute is not null)
                    {
                        return jsTypeInfoAttribute.JsType;
                    }

                    var propertyType = propertyInfo.PropertyType;
                    if (propertyType == typeof(string))
                    {
                        return JsType.String;
                    }

                    if (propertyType.In(typeof(short), typeof(short?), typeof(int), typeof(int?), typeof(double), typeof(double?), typeof(long), typeof(long?)))
                    {
                        return JsType.Number;
                    }

                    if (propertyType.In(typeof(bool), typeof(bool?)))
                    {
                        return JsType.Boolean;
                    }

                    if (propertyType.In(typeof(DateTime), typeof(DateTime?)))
                    {
                        return JsType.Date;
                    }

                    if (propertyType == typeof(IEnumerable) || typeof(IEnumerable).IsSubclassOf(propertyType))
                    {
                        return JsType.Array;
                    }

                    return new NotImplementedException(propertyType.FullName);
                }
            }
        }
    }

    static IReadOnlyList<PluginMethod> GetPluginMethods<AttributeType>()
    {
        var items =
            from assembly in Plugins
            from type in assembly.GetTypes()
            from methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            where methodInfo.GetCustomAttribute(typeof(AttributeType)) is not null
            select (PluginMethod)Delegate.CreateDelegate(typeof(PluginMethod), methodInfo);

        return items.ToList();
    }

    static IAsyncEnumerable<Result<string>> GetStringSuggestions(PropSuggestionScope scope)
    {
        return
            from method in GetStringSuggestionsMethods
            from items in method(scope)
            from item in items
            select item;
    }

    static T RunPluginMethods<T>(IReadOnlyList<PluginMethod> pluginMethods, Scope scope, ScopeKey<T> returnKey) where T : class
    {
        foreach (var method in pluginMethods)
        {
            var response = method(scope);
            if (response.Has(returnKey))
            {
                return returnKey[response];
            }
        }

        return null;
    }
    
    record ComponentMeta
    {
        public IReadOnlyList<PropMeta> Props { get; init; }

        public string TagName { get; init; }
    }

    record PropMeta
    {
        public string Name { get; init; }

        public JsType ValueType { get; init; }
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class SuggestionsAttribute : Attribute
{
    public SuggestionsAttribute(string[] suggestions)
    {
        Suggestions = suggestions;
    }

    public SuggestionsAttribute(string suggestions)
    {
        Suggestions = suggestions.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
    }

    public IReadOnlyList<string> Suggestions { get; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class NodeAnalyzerAttribute : Attribute
{
}

