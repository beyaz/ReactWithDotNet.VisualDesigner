using System.IO;
using System.Text;

namespace ReactWithDotNet.VisualDesigner.Models;

static class Exporter_For_NextJs_with_Tailwind
{
    public static async Task<Result> Export(ApplicationState state)
    {
        var result = await CalculateExportInfo(state);
        if (result.HasError)
        {
            return result.Error;
        }

        var (filePath, fileContent) = result.Value;

        return await IO.TryWriteToFile(filePath, fileContent);
    }

    static IReadOnlyList<string> CalculateElementTreeTsxCodes(ComponentEntity component, string userName)
    {
        var rootVisualElement = DeserializeFromJson<VisualElementModel>(component.RootElementAsJson ?? "");

        var rootNode = ConvertVisualElementModelToReactNodeModel((component, userName), rootVisualElement);

        return ConvertReactNodeModelToTsxCode(rootNode, null, 2);
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

        var filePath = $"{GetExportFolderPath()}{state.ComponentName}.tsx";

        string fileNewContent;
        {
            string[] fileContentInDirectory;
            {
                var result = await IO.TryReadFile(filePath);
                if (result.HasError)
                {
                    return result.Error;
                }

                fileContentInDirectory = result.Value;
            }

            var linesToInject = CalculateElementTreeTsxCodes(component, state.UserName);

            string injectedVersion;
            {
                var newVersion = InjectRender(fileContentInDirectory, linesToInject);
                if (newVersion.HasError)
                {
                    return newVersion.Error;
                }

                injectedVersion = newVersion.Value;
            }

            fileNewContent = injectedVersion;
        }

        return (filePath, fileNewContent);
    }

    static IReadOnlyList<string> ConvertReactNodeModelToTsxCode(ReactNode node, ReactNode parentNode, int indentLevel)
    {
        List<string> lines = [];

        var nodeTag = node.Tag;

        if (nodeTag is null)
        {
            if (node.Text.HasValue())
            {
                lines.Add(Indent(indentLevel) + node.Text);
                return lines;
            }

            throw new ArgumentNullException(nameof(nodeTag));
        }

        var showIf = node.Properties.FirstOrDefault(x => x.Name is "show-if");
        var hideIf = node.Properties.FirstOrDefault(x => x.Name is "hide-if");

        if (showIf is not null)
        {
            node.Properties.Remove(showIf);

            lines.Add($"{Indent(indentLevel)}{{{ClearConnectedValue(showIf.Value)} && (");
            indentLevel++;

            var innerLines = ConvertReactNodeModelToTsxCode(node, parentNode, indentLevel);

            lines.AddRange(innerLines);

            indentLevel--;
            lines.Add($"{Indent(indentLevel)})}}");

            return lines;
        }

        if (hideIf is not null)
        {
            node.Properties.Remove(hideIf);

            lines.Add($"{Indent(indentLevel)}{{!{ClearConnectedValue(hideIf.Value)} && (");
            indentLevel++;

            var innerLines = ConvertReactNodeModelToTsxCode(node, parentNode, indentLevel);

            lines.AddRange(innerLines);

            indentLevel--;
            lines.Add($"{Indent(indentLevel)})}}");

            return lines;
        }

        // is map
        {
            var itemsSource = parentNode?.Properties.FirstOrDefault(x => x.Name is "-items-source");
            if (itemsSource is not null)
            {
                parentNode.Properties.Remove(itemsSource);

                lines.Add($"{Indent(indentLevel)}{{{ClearConnectedValue(itemsSource.Value)}.map((item, index) => {{");
                indentLevel++;
                lines.Add(Indent(indentLevel) + "const isFirst = index === 0;");
                lines.Add(Indent(indentLevel) + $"const isLast = index === {ClearConnectedValue(itemsSource.Value)}.length - 1;");

                lines.Add(string.Empty);
                lines.Add(Indent(indentLevel) + "return (");
                indentLevel++;

                {
                    var innerLines = ConvertReactNodeModelToTsxCode(node, parentNode, indentLevel);
                    lines.AddRange(innerLines);
                }

                indentLevel--;
                lines.Add(Indent(indentLevel) + ");");

                indentLevel--;
                lines.Add(Indent(indentLevel) + "})}");

                return lines;
            }
        }

        var tag = nodeTag.Split('/').Last();

        var indent = new string(' ', indentLevel * 4);

        var sb = new StringBuilder();

        sb.Append($"{indent}<{tag}");

        var childrenProperty = node.Properties.FirstOrDefault(x => x.Name == "children");
        if (childrenProperty is not null)
        {
            node.Properties.Remove(childrenProperty);
        }

        var bindProperty = node.Properties.FirstOrDefault(x => x.Name == "-bind");
        if (bindProperty is not null)
        {
            node.Properties.Remove(bindProperty);
        }

        foreach (var reactProperty in node.Properties)
        {
            var propertyName = reactProperty.Name;

            var propertyValue = reactProperty.Value;

            if (propertyName is "-items-source" || propertyName is "-items-source-design-time-count")
            {
                continue;
            }

            if (propertyValue != null && propertyValue[0] == '"')
            {
                sb.Append($" {propertyName}={propertyValue}");
                continue;
            }

            if (IsConnectedValue(propertyValue))
            {
                sb.Append($" {propertyName}={propertyValue}");
                continue;
            }

            sb.Append($" {propertyName}={{{propertyValue}}}");
        }

        var hasSelfClose = node.Children.Count == 0 && node.Text.HasNoValue() && childrenProperty is null;
        if (hasSelfClose)
        {
            sb.Append("/>");
            lines.Add(sb.ToString());
            return lines;
        }

        // try add from state 
        {
            // sample: children: state.suggestionNodes

            if (childrenProperty is not null)
            {
                sb.Append(">");
                lines.Add(sb.ToString());

                lines.Add($"{Indent(indentLevel + 1)}{childrenProperty.Value}");

                // Close tag
                lines.Add($"{indent}</{tag}>");

                return lines;
            }
        }

        // from props
        {
            if (node.Children.Count == 1)
            {
                var childrenText = node.Children[0].Text + string.Empty;
                if (bindProperty is not null)
                {
                    childrenText = bindProperty.Value;
                }

                if (IsConnectedValue(childrenText))
                {
                    sb.Append(">");
                    lines.Add(sb.ToString());

                    lines.Add($"{Indent(indentLevel + 1)}{childrenText}");

                    // Close tag
                    lines.Add($"{indent}</{tag}>");

                    return lines;
                }
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
            lines.AddRange(ConvertReactNodeModelToTsxCode(child, node, indentLevel + 1));
        }

        // Close tag
        lines.Add($"{indent}</{tag}>");

        return lines;
    }

    static ReactNode ConvertVisualElementModelToReactNodeModel((ComponentEntity component, string userName) context, VisualElementModel element)
    {
        var (component, userName) = context;

        List<string> classNames = [];

        // Open tag
        var tag = element.Tag;

        if (tag == "a")
        {
            tag = "Link";
        }

        if (tag == "img")
        {
            tag = "Image";

            if (element.Properties.Any(x => x.Contains("alt:")) is false)
            {
                element.Properties.Add("alt: -");
            }
        }

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

                if (IsConnectedValue(value) || value.StartsWith("props.") || value.StartsWith("state."))
                {
                    node.Properties.Add(new() { Name = name, Value = value });
                    continue;
                }

                if (tag == "Image")
                {
                    value = "/" + value; // todo: fixme
                }

                var componentInProject = GetComponenUserOrMainVersion(component.ProjectId, tag, userName);
                if (componentInProject is not null)
                {
                    node.Properties.Add(new() { Name = name, Value = $"()=>invokeLogic('{value}')" });
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
                                    Value = $"({partPrm})=>invokeLogic('{value}', {partPrm})"
                                });
                            }
                            else
                            {
                                node.Properties.Add(new()
                                {
                                    Name  = name,
                                    Value = $"()=>invokeLogic('{value}')"
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
                        case "outline":
                        {
                            classNames.Add($"{name}-{value}");
                            continue;
                        }

                        case "text-decoration":
                        {
                            classNames.Add($"{value}");
                            continue;
                        }

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

                        case "text-align":
                        {
                            classNames.Add($"text-{value}");
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
            return node;
        }

        if (IsConnectedValue(element.Text) || (element.Text + string.Empty).StartsWith("props.") || (element.Text + string.Empty).StartsWith("state."))
        {
            node.Children.Add(new() { Text = element.Text });

            return node;
        }

        // Add text content
        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            node.Children.Add(new() { Text = $"{{t(\"{element.Text}\")}}" });
        }

        // Add children
        foreach (var child in element.Children)
        {
            var childNode = ConvertVisualElementModelToReactNodeModel(context, child);

            node.Children.Add(childNode);
        }

        return node;
    }

    static string GetExportFolderPath()
    {
        return "C:\\github\\hopgogo\\web\\enduser-ui\\src\\components\\";
    }

    static string Indent(int indentLevel)
    {
        return new(' ', indentLevel * 4);
    }

    static Result<string> InjectRender(IReadOnlyList<string> fileContent, IReadOnlyList<string> linesToInject)
    {
        var lines = fileContent.ToList();

        var firstReturnLineIndex = lines.FindIndex(l => l == "    return (");
        if (firstReturnLineIndex < 0)
        {
            return new InvalidOperationException("No return found");
        }

        var firstReturnCloseLineIndex = lines.FindIndex(firstReturnLineIndex, l => l == "    );");
        if (firstReturnCloseLineIndex < 0)
        {
            return new InvalidOperationException("Return close not found");
        }

        lines.RemoveRange(firstReturnLineIndex + 1, firstReturnCloseLineIndex - firstReturnLineIndex - 1);

        lines.InsertRange(firstReturnLineIndex + 1, linesToInject);

        var injectedFileContent = string.Join(Environment.NewLine, lines);

        return injectedFileContent;
    }

    class IO
    {
        public static async Task<Result<string[]>> TryReadFile(string filePath)
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

    record ReactNode
    {
        public List<ReactNode> Children { get; } = [];

        public List<ReactProperty> Properties { get; } = [];

        public string Tag { get; init; }

        public string Text { get; init; }
    }

    record ReactProperty
    {
        public string Name { get; init; }
        public string Value { get; init; }
    }
}