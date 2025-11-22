using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using ReactWithDotNet.VisualDesigner.Configuration;

namespace ReactWithDotNet.VisualDesigner;

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

static class TsImport
{
    public static string ToString(string name, string package)
    {
        return $"import {{ {name} }} from \"{package}\";";
    }
    
    public static string ToString(IReadOnlyList<(string Name, string Package)> imports)
    {
        return string.Join(Environment.NewLine, from x in imports select ToString(x.Name, x.Package));
    }
}

public sealed class TsImportCollection : IEnumerable<(string Name, string Package)>
{
    readonly List<(string Name, string Package)> items = [];        
    
    public void Add(string name, string package)
    {
        if (items.Any(x=>x.Name == name && x.Package == package))
        {
            return;
        }
        
        items.Add((name, package));
    }
    
    
    public void Add(TsImportCollection tsImportCollection)
    {
        foreach (var item in tsImportCollection)
        {
            Add(item.Name, item.Package);
        }
    }
    
    public void Add(IEnumerable<TsImportCollection> tsImportCollection)
    {
        foreach (var item in tsImportCollection)
        {
            Add(item);
        }
    }
    
   

    public IEnumerator<(string Name, string Package)> GetEnumerator()
    {
        return items.GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return TsImport.ToString(items);
    }

    public IReadOnlyList<string> ToTsLines()
    {
        return new List<string>
        {
            from x in items select TsImport.ToString(x.Name, x.Package)
        };
    }

   
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
public sealed class AfterReadConfigAttribute : Attribute
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