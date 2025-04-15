using System.Globalization;
using System.IO;
using System.Text;

namespace ReactWithDotNet.VisualDesigner.Models;

static class Exporter_For_NextJs_with_Tailwind
{
    static readonly CultureInfo CultureInfo_en_US = new("en-US");

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

    static string AsPixel(this double value)
    {
        return value.ToString(CultureInfo_en_US) + "px";
    }

    static async Task<IReadOnlyList<string>> CalculateElementTreeTsxCodes(ComponentEntity component, string userName)
    {
        var rootVisualElement = DeserializeFromJson<VisualElementModel>(component.RootElementAsJson ?? "");

        var rootNode = await ConvertVisualElementModelToReactNodeModel((component, userName), rootVisualElement);

        return ConvertReactNodeModelToTsxCode(rootNode, null, 2);
    }

    static async Task<Result<(string filePath, string fileContent)>> CalculateExportInfo(ApplicationState state)
    {
        ComponentEntity component;
        {
            var result = await GetComponenUserOrMainVersionAsync(state.ProjectId, state.ComponentName, state.UserName);
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

            var linesToInject = await CalculateElementTreeTsxCodes(component, state.UserName);

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

    static async Task<ReactNode> ConvertVisualElementModelToReactNodeModel((ComponentEntity component, string userName) context, VisualElementModel element)
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
                element.Properties.Add("alt: ?");
            }

            var sizeProperty = element.Properties.FirstOrDefault(x => x.Contains("size:"));
            if (sizeProperty is not null)
            {
                element.Properties.Remove(sizeProperty);

                element.Properties.Add(sizeProperty.Replace("size:", "width:"));

                element.Properties.Add(sizeProperty.Replace("size:", "height:"));
            }
            else
            {
                var hasNoWidthDecleration = element.Properties.Any(x => x.Contains("w:") || x.Contains("width:")) is false;
                var hasNoHeightDecleration = element.Properties.Any(x => x.Contains("h:") || x.Contains("height:")) is false;
                var hasNoFilltDecleration = element.Properties.Any(x => x.Contains("fill:")) is false;

                if (hasNoWidthDecleration && hasNoHeightDecleration && hasNoFilltDecleration)
                {
                    element.Properties.Add("fill: {true}");
                }
            }

            // try to add width and height to default style
            {
                // width
                {
                    var propertyValue = element.Properties.TryGetPropertyValue("width", "w");
                    if (propertyValue is not null)
                    {
                        var defaultStyle = element.StyleGroups.FirstOrDefault(x => x.Condition == "*");
                        if (defaultStyle is null)
                        {
                            element.StyleGroups.Add(new() { Condition = "*", Items = [$"width: {propertyValue}"] });
                        }
                        else
                        {
                            if (defaultStyle.Items.TryGetPropertyValue("width", "w") is null)
                            {
                                defaultStyle.Items.Add($"width: {propertyValue}");
                            }
                        }
                    }
                }

                // height
                {
                    var propertyValue = element.Properties.TryGetPropertyValue("height", "h");
                    if (propertyValue is not null)
                    {
                        var defaultStyle = element.StyleGroups.FirstOrDefault(x => x.Condition == "*");
                        if (defaultStyle is null)
                        {
                            element.StyleGroups.Add(new() { Condition = "*", Items = [$"height: {propertyValue}"] });
                        }
                        else
                        {
                            if (defaultStyle.Items.TryGetPropertyValue("height", "h") is null)
                            {
                                defaultStyle.Items.Add($"height: {propertyValue}");
                            }
                        }
                    }
                }
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

                //if (tag == "Image")
                //{
                //    value = "/" + value; // todo: fixme
                //}

                var componentInProject = (await GetComponenUserOrMainVersionAsync(component.ProjectId, tag, userName)).Value;
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
            if (styleGroup.Condition == "*")
            {
                foreach (var styleItem in styleGroup.Items)
                {
                    var tailwindClassName = Css.ConvertDesignerStyleItemToTailwindClassName(styleItem);

                    classNames.Add(tailwindClassName);
                }

                continue;
            }

            throw new NotImplementedException($"Condition not handled: {styleGroup.Condition}");
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
            var childNode = await ConvertVisualElementModelToReactNodeModel(context, child);

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

    static class Css
    {
        public static string ConvertDesignerStyleItemToTailwindClassName(string designerStyleItem)
        {
            var (success, name, value) = TryParsePropertyValue(designerStyleItem);
            if (success)
            {
                return ConvertToTailwindClass(name, value);
            }

            return designerStyleItem;
        }

        public static string ConvertToTailwindClass(string name, string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var isValueDouble = double.TryParse(value, out var valueAsDouble);

            switch (name)
            {
                case "outline":
                {
                    return $"{name}-{value}";
                }

                case "text-decoration":
                {
                    return $"{value}";
                }

                case "W":
                case "w":
                case "width":
                {
                    if (value == "fit-content")
                    {
                        return "w-fit";
                    }

                    if (isValueDouble)
                    {
                        value = valueAsDouble.AsPixel();
                    }

                    return $"w-[{value}]";
                }

                case "text-align":
                {
                    return $"text-{value}";
                }

                case "h":
                case "height":
                {
                    if (value == "fit-content")
                    {
                        return "h-fit";
                    }

                    if (isValueDouble)
                    {
                        value = valueAsDouble.AsPixel();
                    }

                    return $"h-[{value}]";
                }

                case "max-width":
                    return $"max-w-[{value}px]";

                case "max-height":
                    return $"max-h-[{value}px]";

                case "min-width":
                    return $"min-w-[{value}px]";

                case "min-height":
                    return $"min-h-[{value}px]";

                case "pt":
                    return $"pt-[{value}px]";

                case "z-index":
                    return $"z-[{value}]";

                case "overflow-y":
                case "overflow-x":
                    return $"{name}-{value}";

                case "pb":
                    return $"pb-[{value}px]";

                case "pl":
                    return $"pl-[{value}px]";

                case "p":
                    return $"p-[{value}px]";

                case "border-top-left-radius":
                    return $"rounded-tl-[{value}px]";

                case "border-top-right-radius":
                    return $"rounded-tr-[{value}px]";

                case "border-bottom-left-radius":
                    return $"rounded-bl-[{value}px]";

                case "border-bottom-right-radius":
                    return $"rounded-br-[{value}px]";

                case "flex-grow":
                    return $"flex-grow-[{value}]";

                case "border-bottom-width":
                    return $"border-b-[{value}px]";

                case "border-top-width":
                    return $"border-t-[{value}px]";

                case "border-left-width":
                    return $"border-l-[{value}px]";

                case "border-right-width":
                    return $"border-r-[{value}px]";

                case "border-top":
                case "border-right":
                case "border-left":
                case "border-bottom":
                {
                    var direction = name.Split('-', StringSplitOptions.RemoveEmptyEntries).Last();

                    var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3)
                    {
                        if (Project.Colors.TryGetValue(parts[2], out var htmlColor))
                        {
                            parts[2] = htmlColor;
                        }

                        var directionShortName = direction switch
                        {
                            "top"    => "t",
                            "bottom" => "b",
                            "right"  => "r",
                            "left"   => "l",
                            _        => throw new ArgumentOutOfRangeException(direction)
                        };

                        return $"border-{directionShortName}-[{parts[0]}]" +
                               $"[border-{direction}-style:{parts[1]}]" +
                               $"[border-{direction}-color:{parts[2]}]";
                    }

                    throw new ArgumentOutOfRangeException(direction);
                }

                case "pr":
                    return $"pr-[{value}px]";

                case "px":
                    return $"px-[{value}px]";

                case "py":
                    return $"py-[{value}px]";

                case "display":
                    return $"{value}";

                case "color":
                {
                    if (Project.Colors.TryGetValue(value, out var htmlColor))
                    {
                        value = htmlColor;
                    }

                    return $"text-[{value}]";
                }
                case "gap":
                    return $"gap-[{value}px]";

                case "size":
                    return $"size-[{value}px]";

                case "bottom":
                case "top":
                case "left":
                case "right":
                    return $"{name}-[{value}px]";

                case "flex-direction" when value == "column":
                    return "flex-col";

                case "align-items":
                    return $"items-{value.RemoveFromStart("align-")}";

                case "justify-content":
                    return $"justify-{value.Split('-').Last()}";

                case "border-radius":
                    return $"rounded-[{value}px]";

                case "font-size":
                    return $"[font-size:{value}px]";

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
                            return "border" +
                                   $"border-[{parts[2]}]";
                        }

                        return $"border-[{parts[0]}]" +
                               $"border-[{parts[1]}]" +
                               $"border-[{parts[2]}]";
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

                    return $"bg-[{value}]";
                }
                case "position":
                    return $"{value}";
            }

            throw new InvalidOperationException($"Css not handled. {name}: {value}");
        }
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