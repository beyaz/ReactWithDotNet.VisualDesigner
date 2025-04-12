using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReactWithDotNet.VisualDesigner.Models;

static class Exporter_For_NextJs_with_Tailwind
{
    const string childrenIdentifier = "[...]";

    public static async Task<Result> Export(ApplicationState state)
    {
        var result = await CalculateExportInfo(state);
        if (result.HasError)
        {
            return result.Error;
        }

        var (filePath, fileContent) = result.Value;

        return await TryWriteToFile(filePath, fileContent);
    }

    static void Add(this List<string> list, int indentLevel, string value)
    {
        list.Add(Indent(indentLevel) + value);
    }

    static (List<string> lines, List<ImportInfo> imports) AsLines(ComponentEntity component, string userName)
    {
        List<ImportInfo> imports = [];

        List<string> lines = [];

        var hasState = false;

        bool hasProps;

        bool hasChildrenDeclerationInProps;

        // TryWriteProps
        {
            var result = TryWriteProps(component, 0);

            hasProps = result.lines.Any();

            if (hasProps)
            {
                lines.Add(string.Empty);

                lines.AddRange(result.lines);

                imports.AddRange(result.imports);
            }

            hasChildrenDeclerationInProps = result.lines.Any(x => x.TrimStart().StartsWith("children", StringComparison.OrdinalIgnoreCase));
        }

        // TryWriteState
        {
            var result = TryWriteState(component, 0);

            lines.AddRange(result.lines);

            imports.AddRange(result.imports);

            if (result.lines.Any())
            {
                imports.Add(new() { ClassName = "useState", Package = "react", IsNamed = true });

                hasState = true;
            }
        }

        lines.Add(string.Empty);
        lines.Add($"export default function {component.Name.Split('/').Last()}({(hasProps ? "props: Props" : string.Empty)}) {{");

        if (hasState)
        {
            imports.Add(new() { ClassName = "initializeState, logic", Package = $"@/components/{component.Name}.Logic", IsNamed = true });

            lines.Add(string.Empty);
            lines.Add($"{Indent(1)}const [state, setState] = useState<State>(() => initializeState(props));");
        }

        {
            var rootVisualElement = DeserializeFromJson<VisualElementModel>(component.RootElementAsJson ?? "");

            var result = ConvertToReactNode((component, userName, hasChildrenDeclerationInProps), rootVisualElement);

            imports.AddRange(result.imports);

            foreach (var line in result.bodyLines.Distinct())
            {
                lines.Add($"{Indent(1)}{line}");
            }

            if (hasState)
            {
                lines.Add("");
                lines.Add(1, "const call = (name: string, ...args: any[]) =>");
                lines.Add(1, "{");
                lines.Add(1, "    setState((prevState: State) =>");
                lines.Add(1, "    {");
                lines.Add(1, "        logic(props, prevState)[name](...args);");
                lines.Add("");
                lines.Add(1, "        return { ...prevState };");
                lines.Add(1, "    });");
                lines.Add(1, "};");
            }

            lines.Add(string.Empty);
            lines.Add($"{Indent(1)}return (");

            WriteTo(lines, hasChildrenDeclerationInProps, result.node, 2);

            lines.Add($"{Indent(1)});");

            lines.Add("}");
        }

        return (lines, imports);
    }

    static IReadOnlyList<string> AsTsLines(this IReadOnlyList<ImportInfo> imports)
    {
        List<string> lines = [];

        foreach (var import in imports.DistinctBy(x => x.Package + x.ClassName))
        {
            if (import.IsNamed)
            {
                lines.Add($"import {{ {import.ClassName} }} from \"{import.Package}\";");
            }
            else
            {
                lines.Add($"import {import.ClassName} from \"{import.Package}\";");
            }
        }

        return lines;
    }

    static async Task<Result<(string filePath, string fileContent)>> CalculateExportInfo(ApplicationState state)
    {
        ComponentEntity component;
        {
            var result = await GetComponenUserOrMainVersion(state);
            if (result.HasError)
            {
                return result.Error;
            }

            component = result.Value;
        }

        string fileContent;
        {
            var result = AsLines(component, state.UserName);

            var fileLines = new List<string>(result.imports.AsTsLines());

            fileLines.AddRange(result.lines);

            fileContent = string.Join(Environment.NewLine, fileLines);
        }

        var filePath = $"{GetExportFolderPath()}{state.ComponentName}.tsx";

        return (filePath, fileContent);
    }

    static (ReactNode node, List<string> bodyLines, List<ImportInfo> imports)
        ConvertToReactNode((ComponentEntity component, string userName, bool hasChildrenDeclerationInProps) context, VisualElementModel element)
    {
        var (component, userName, hasChildrenDeclerationInProps) = context;

        List<ImportInfo> imports = [];

        List<string> bodyLines = [];

        List<string> classNames = [];

        // Open tag
        var tag = element.Tag;

        if (tag == "a")
        {
            imports.Add(new() { ClassName = "Link", Package = "next/link" });
            tag = "Link";
        }

        if (tag == "img")
        {
            imports.Add(new() { ClassName = "Image", Package = "next/image" });

            tag = "Image";
        }

        imports.Add(GetTagImportInfo(component.ProjectId, userName, tag));

        var node = new ReactNode { Tag = tag };

        // Add properties
        foreach (var property in element.Properties)
        {
            var (success, name, value) = TryParsePropertyValue(property);
            if (success)
            {
                if (name == "class")
                {
                    classNames.AddRange(value.Split(" ", StringSplitOptions.RemoveEmptyEntries));
                    continue;
                }

                if (name == "w" || name == "width")
                {
                    node.Properties.Add(new() { Name = "width", Value = value });
                    continue;
                }

                if (name == "h" || name == "height")
                {
                    node.Properties.Add(new() { Name = "height", Value = value });
                    continue;
                }

                if (tag == "Image")
                {
                    value = "/" + value; // todo: fixme
                }

                if (value.StartsWith("props.") || value.StartsWith("state."))
                {
                    node.Properties.Add(new() { Name = name, Value = value });
                    continue;
                }

                var componentInProject = GetComponenUserOrMainVersion(component.ProjectId, tag, userName);
                if (componentInProject is not null)
                {
                    node.Properties.Add(new() { Name = name, Value = $"()=>call('{value}')" });
                    continue;
                }

                // process as external
                {
                    var isConnectedToExternalComponent = false;

                    foreach (var externalComponent in Project.ExternalComponents.Where(x => x.Name == tag))
                    {
                        var @event = externalComponent.Events.FirstOrDefault(e => e.Name == name);
                        if (@event is not null)
                        {
                            isConnectedToExternalComponent = true;

                            if (@event.Parameters.Any())
                            {
                                var partPrm = string.Join(", ", @event.Parameters.Select(p => p.Name));

                                node.Properties.Add(new()
                                {
                                    Name  = name,
                                    Value = $"({partPrm})=>call('{name}', {partPrm})"
                                });
                            }
                            else
                            {
                                node.Properties.Add(new()
                                {
                                    Name  = name,
                                    Value = $"()=>call('{name}')"
                                });
                            }

                            break;
                        }
                    }

                    if (isConnectedToExternalComponent)
                    {
                        continue;
                    }
                }

                node.Properties.Add(new() { Name = name, Value = '"' + value + '"' });
            }
        }

        foreach (var styleGroup in element.StyleGroups)
        {
            foreach (var styleItem in styleGroup.Items)
            {
                var (success, name, value) = TryParsePropertyValue(styleItem);
                if (success)
                {
                    switch (name)
                    {
                        case "W":
                        case "w":
                        case "width":
                        {
                            if (value == "fit-content")
                            {
                                classNames.Add("w-fit");
                                continue;
                            }

                            classNames.Add($"w-[{value}px]");
                            continue;
                        }
                        case "h":
                        case "height":
                        {
                            if (value == "fit-content")
                            {
                                classNames.Add("h-fit");
                                continue;
                            }

                            classNames.Add($"h-[{value}px]");
                            continue;
                        }

                        case "max-width":
                            classNames.Add($"max-w-[{value}px]");
                            continue;
                        case "max-height":
                            classNames.Add($"max-h-[{value}px]");
                            continue;
                        case "min-width":
                            classNames.Add($"min-w-[{value}px]");
                            continue;
                        case "min-height":
                            classNames.Add($"min-h-[{value}px]");
                            continue;
                        case "pt":
                            classNames.Add($"pt-[{value}px]");
                            continue;
                        case "z-index":
                            classNames.Add($"z-[{value}]");
                            continue;
                        case "overflow-y":
                        case "overflow-x":
                            classNames.Add($"{name}-{value}");
                            continue;
                        case "pb":
                            classNames.Add($"pb-[{value}px]");
                            continue;
                        case "pl":
                            classNames.Add($"pl-[{value}px]");
                            continue;
                        case "p":
                            classNames.Add($"p-[{value}px]");
                            continue;
                        case "border-top-left-radius":
                            classNames.Add($"rounded-tl-[{value}px]");
                            continue;
                        case "border-top-right-radius":
                            classNames.Add($"rounded-tr-[{value}px]");
                            continue;
                        case "border-bottom-left-radius":
                            classNames.Add($"rounded-bl-[{value}px]");
                            continue;
                        case "border-bottom-right-radius":
                            classNames.Add($"rounded-br-[{value}px]");
                            continue;
                        case "flex-grow":
                            classNames.Add($"flex-grow-[{value}]");
                            continue;
                        case "border-bottom-width":
                            classNames.Add($"border-b-[{value}px]");
                            continue;
                        case "border-top-width":
                            classNames.Add($"border-t-[{value}px]");
                            continue;
                        case "border-left-width":
                            classNames.Add($"border-l-[{value}px]");
                            continue;
                        case "border-right-width":
                            classNames.Add($"border-r-[{value}px]");
                            continue;
                        case "pr":
                            classNames.Add($"pr-[{value}px]");
                            continue;
                        case "px":
                            classNames.Add($"px-[{value}px]");
                            continue;
                        case "py":
                            classNames.Add($"py-[{value}px]");
                            continue;
                        case "display":
                            classNames.Add($"{value}");
                            continue;
                        case "color":
                        {
                            if (Project.Colors.TryGetValue(value, out var htmlColor))
                            {
                                value = htmlColor;
                            }

                            classNames.Add($"text-[{value}]");
                            continue;
                        }
                        case "gap":
                            classNames.Add($"gap-[{value}px]");
                            continue;
                        case "size":
                            classNames.Add($"size-[{value}px]");
                            continue;
                        case "bottom":
                        case "top":
                        case "left":
                        case "right":
                            classNames.Add($"{name}-[{value}px]");
                            continue;
                        case "flex-direction" when value == "column":
                            classNames.Add("flex-col");
                            continue;
                        case "align-items":
                            classNames.Add($"items-{value.RemoveFromStart("align-")}");
                            continue;
                        case "justify-content":
                            classNames.Add($"justify-{value.Split('-').Last()}");
                            continue;
                        case "border-radius":
                            classNames.Add($"rounded-[{value}px]");
                            continue;
                        case "font-size":
                            classNames.Add($"[font-size:{value}px]");
                            continue;
                        case "border":
                        {
                            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 3)
                            {
                                if (Project.Colors.TryGetValue(parts[2], out var htmlColor))
                                {
                                    parts[2] = htmlColor;
                                }

                                if (parts[0] == "1px" && parts[1] == "solid")
                                {
                                    classNames.Add("border");
                                    classNames.Add($"border-[{parts[2]}]");
                                    continue;
                                }

                                classNames.Add($"border-[{parts[0]}]");
                                classNames.Add($"border-[{parts[1]}]");
                                classNames.Add($"border-[{parts[2]}]");

                                continue;
                            }

                            break;
                        }
                        case "background":
                        case "bg":
                        {
                            if (Project.Colors.TryGetValue(value, out var htmlColor))
                            {
                                value = htmlColor;
                            }

                            classNames.Add($"bg-[{value}]");
                            continue;
                        }
                        case "position":
                            classNames.Add($"{value}");
                            continue;
                    }
                }

                classNames.Add(styleItem);
            }
        }

        if (classNames.Any())
        {
            node.Properties.Add(new() { Name = "className", Value = '"' + string.Join(" ", classNames) + '"' });
        }

        var hasSelfClose = element.Children.Count == 0 && element.Text.HasNoValue();
        if (hasSelfClose)
        {
            return (node, bodyLines, imports);
        }

        if (element.Text == childrenIdentifier && hasChildrenDeclerationInProps)
        {
            node.Children.Add(new() { Text = element.Text });

            return (node, bodyLines, imports);
        }

        if ((element.Text + string.Empty).StartsWith("props.") || (element.Text + string.Empty).StartsWith("state."))
        {
            node.Children.Add(new() { Text = element.Text });

            return (node, bodyLines, imports);
        }

        // Add text content
        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            if (imports.All(x => x.ClassName != "useTranslations"))
            {
                imports.Add(new() { ClassName = "useTranslations", Package = "next-intl", IsNamed = true });

                bodyLines.Add(string.Empty);
                bodyLines.Add("const t = useTranslations();");
            }

            node.Children.Add(new() { Text = $"{{t(\"{element.Text}\")}}" });
        }

        // Add children
        foreach (var child in element.Children)
        {
            ReactNode childNode;
            {
                var result = ConvertToReactNode(context, child);

                childNode = result.node;

                bodyLines.AddRange(result.bodyLines);
                imports.AddRange(result.imports);
            }

            node.Children.Add(childNode);
        }

        return (node, bodyLines, imports);
    }

    static string GetExportFolderPath()
    {
        return "C:\\github\\hopgogo\\web\\enduser-ui\\src\\components\\";
    }

    static ImportInfo GetTagImportInfo(int projectId, string userName, string tag)
    {
        var component = GetComponenUserOrMainVersion(projectId, tag, userName);
        if (component is not null)
        {
            return new() { ClassName = tag.Split('/').Last(), Package = $"@/components/{tag}" };
        }

        if (tag == "Popover" || tag == "PopoverTrigger" || tag == "PopoverContent" || tag == "heroui/Checkbox")
        {
            return new() { ClassName = tag.Split('/').Last(), Package = "@heroui/react", IsNamed = true };
        }

        return null;
    }

    static string Indent(int indentLevel)
    {
        return new(' ', indentLevel * 4);
    }

    static (List<string> lines, List<ImportInfo> imports) TryWriteInterfaceBody(int indent, string yaml)
    {
        List<string> lines = [];

        List<ImportInfo> imports = [];

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yamlObject = deserializer.Deserialize<Dictionary<string, object>>(yaml);

        foreach (var kvp in yamlObject)
        {
            var propertyName = kvp.Key;
            var tsTypeName = InferTsTypeName(kvp.Value?.ToString());

            var isNullable = propertyName.TrimEnd().EndsWith("?");

            if (isNullable)
            {
                propertyName = propertyName.RemoveFromEnd("?");
            }

            lines.Add($"{Indent(indent)}{propertyName}{(isNullable ? "?" : string.Empty)}: {tsTypeName};");
        }

        return (lines, imports);

        string InferTsTypeName(string value)
        {
            if (value == null)
            {
                return "string";
            }

            value = value.Trim();

            if (bool.TryParse(value, out _))
            {
                return "boolean";
            }

            if (int.TryParse(value, out _))
            {
                return "number";
            }

            if (value.StartsWith("() =>"))
            {
                return value;
            }

            if (value.Equals("ReactNode", StringComparison.OrdinalIgnoreCase))
            {
                imports.Add(new() { ClassName = "React", Package = "react" });

                return "React.ReactNode";
            }

            return "string";
        }
    }

    static (List<string> lines, List<ImportInfo> imports) TryWriteProps(ComponentEntity cmp, int indent)
    {
        List<string> lines = [];

        List<ImportInfo> imports = [];

        if (cmp.PropsAsYaml.HasNoValue())
        {
            return (lines, imports);
        }

        lines.Add($"{Indent(indent)}export interface Props {{");

        indent++;

        // body
        {
            var result = TryWriteInterfaceBody(indent, cmp.PropsAsYaml);

            lines.AddRange(result.lines);

            imports.AddRange(result.imports);
        }

        indent--;

        lines.Add($"{Indent(indent)}}}");

        return (lines, imports);
    }

    static (List<string> lines, List<ImportInfo> imports) TryWriteState(ComponentEntity cmp, int indent)
    {
        List<string> lines = [];

        List<ImportInfo> imports = [];

        if (cmp.StateAsYaml.HasNoValue())
        {
            return (lines, imports);
        }

        lines.Add($"{Indent(indent)}export interface State {{");

        indent++;

        // body
        {
            var result = TryWriteInterfaceBody(indent, cmp.StateAsYaml);

            lines.AddRange(result.lines);

            imports.AddRange(result.imports);
        }

        indent--;

        lines.Add($"{Indent(indent)}}}");

        return (lines, imports);
    }

    static async Task<Result> TryWriteToFile(string filePath, string fileContent)
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

    static void WriteTo(List<string> lines, bool hasChildrenDeclerationInProps, ReactNode node, int indentLevel)
    {
        var nodeTag = node.Tag;

        if (nodeTag is null)
        {
            if (node.Text.HasValue())
            {
                lines.Add(Indent(indentLevel) + node.Text);
                return;
            }

            throw new ArgumentNullException(nameof(nodeTag));
        }

        var tag = nodeTag.Split('/').Last();

        var indent = new string(' ', indentLevel * 4);

        var sb = new StringBuilder();

        sb.Append($"{indent}<{tag}");

        foreach (var reactProperty in node.Properties)
        {
            var propertyName = reactProperty.Name;

            var propertyValue = reactProperty.Value;

            if (propertyValue != null && propertyValue[0] == '"')
            {
                sb.Append($" {propertyName}={propertyValue}");
                continue;
            }

            sb.Append($" {propertyName}={{{propertyValue}}}");
        }

        var hasSelfClose = node.Children.Count == 0 && node.Text.HasNoValue();
        if (hasSelfClose)
        {
            sb.Append("/>");
            lines.Add(sb.ToString());
            return;
        }

        // try add as children
        {
            if (hasChildrenDeclerationInProps)
            {
                if (node.Children.Count == 1 && node.Children[0].Text == childrenIdentifier)
                {
                    sb.Append(">");
                    lines.Add(sb.ToString());

                    lines.Add($"{Indent(indentLevel + 1)}{{ props.children }}");

                    // Close tag
                    lines.Add($"{indent}</{tag}>");

                    return;
                }
            }
        }

        // from props
        {
            if ((node.Children.Count == 1 && (node.Children[0].Text + string.Empty).StartsWith("props.")) || (node.Children[0].Text + string.Empty).StartsWith("state."))
            {
                sb.Append(">");
                lines.Add(sb.ToString());

                lines.Add($"{Indent(indentLevel + 1)}{{ {node.Children[0].Text} }}");

                // Close tag
                lines.Add($"{indent}</{tag}>");

                return;
            }
        }

        sb.Append(">");
        lines.Add(sb.ToString());

        // Add text content
        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            lines.Add($"{indent}{{{node.Text})}}");
        }

        // Add children
        foreach (var child in node.Children)
        {
            WriteTo(lines, hasChildrenDeclerationInProps, child, indentLevel + 1);
        }

        // Close tag
        lines.Add($"{indent}</{tag}>");
    }

    class ReactNode
    {
        public List<ReactNode> Children { get; } = [];

        public List<ReactProperty> Properties { get; } = [];
        
        public string Tag { get; init; }

        public string Text { get; init; }
    }

    class ReactProperty
    {
        public string Name { get; init; }
        public string Value { get; init; }
    }

    record ImportInfo
    {
        public string ClassName { get; init; }
        public bool IsNamed { get; init; }
        public string Package { get; init; }
    }
}