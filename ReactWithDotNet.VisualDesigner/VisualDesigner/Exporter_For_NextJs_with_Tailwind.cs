using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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

    static async Task<Result<IReadOnlyList<string>>> CalculateElementTreeTsxCodes(ComponentEntity component, string userName)
    {
        var rootVisualElement = component.RootElementAsJson.AsVisualElementModel();

        ReactNode rootNode;
        {
            var result = await ConvertVisualElementModelToReactNodeModel((component, userName), rootVisualElement);
            if (result.HasError)
            {
                return result.Error;
            }

            rootNode = result.Value;
        }

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

            IReadOnlyList<string> linesToInject;
            {
                var result = await CalculateElementTreeTsxCodes(component, state.UserName);
                if (result.HasError)
                {
                    return result.Error;
                }

                linesToInject = result.Value;
            }

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

    static Result<IReadOnlyList<string>> ConvertReactNodeModelToTsxCode(ReactNode node, ReactNode parentNode, int indentLevel)
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

            return new ArgumentNullException(nameof(nodeTag));
        }

        var showIf = node.Properties.FirstOrDefault(x => x.Name is "-show-if");
        var hideIf = node.Properties.FirstOrDefault(x => x.Name is "-hide-if");

        if (showIf is not null)
        {
            node.Properties.Remove(showIf);

            lines.Add($"{Indent(indentLevel)}{{{ClearConnectedValue(showIf.Value)} && (");
            indentLevel++;

            IReadOnlyList<string> innerLines;
            {
                var result = ConvertReactNodeModelToTsxCode(node, parentNode, indentLevel);
                if (result.HasError)
                {
                    return result.Error;
                }

                innerLines = result.Value;
            }

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

            IReadOnlyList<string> innerLines;
            {
                var result = ConvertReactNodeModelToTsxCode(node, parentNode, indentLevel);
                if (result.HasError)
                {
                    return result.Error;
                }

                innerLines = result.Value;
            }

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

                lines.Add($"{Indent(indentLevel)}{{");
                indentLevel++;
                
                
                lines.Add($"{Indent(indentLevel)}{ClearConnectedValue(itemsSource.Value)}.map((_item, _index) =>");
                lines.Add($"{Indent(indentLevel)}{{");
                indentLevel++;

                lines.Add(Indent(indentLevel) + "return (");
                indentLevel++;

                IReadOnlyList<string> innerLines;
                {
                    var result = ConvertReactNodeModelToTsxCode(node, parentNode, indentLevel);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    innerLines = result.Value;
                }
                lines.AddRange(innerLines);

                indentLevel--;
                lines.Add(Indent(indentLevel) + ");");

                indentLevel--;
                lines.Add(Indent(indentLevel) + "})");
                
                indentLevel--;
                lines.Add(Indent(indentLevel) + "}");

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
            IReadOnlyList<string> childTsx;
            {
                var result = ConvertReactNodeModelToTsxCode(child, node, indentLevel + 1);
                if (result.HasError)
                {
                    return result.Error;
                }

                childTsx = result.Value;
            }

            lines.AddRange(childTsx);
        }

        // Close tag
        lines.Add($"{indent}</{tag}>");

        return lines;
    }

    static async Task<Result<ReactNode>> ConvertVisualElementModelToReactNodeModel((ComponentEntity component, string userName) context, VisualElementModel element)
    {
        var (component, userName) = context;

        List<string> classNames = [];

        var classNameShouldBeTemplateLiteral = false;

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
                        if (element.Styles.TryGetPropertyValue("width", "w") is null)
                        {
                            element.Styles.Add($"width: {propertyValue}");
                        }
                    }
                }

                // height
                {
                    var propertyValue = element.Properties.TryGetPropertyValue("height", "h");
                    if (propertyValue is not null)
                    {
                        if (element.Styles.TryGetPropertyValue("height", "h") is null)
                        {
                            element.Styles.Add($"height: {propertyValue}");
                        }
                    }
                }
            }
        }

        var node = new ReactNode { Tag = tag };

        // Add properties
        foreach (var property in element.Properties)
        {
            var parseResult = TryParsePropertyValue(property);
            if (parseResult.success)
            {
                var name = parseResult.name;
                var value = parseResult.value;
                
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

                if (IsConnectedValue(value) || value.StartsWith("props.") || value.StartsWith("state.") || IsStringValue(value))
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

        
        foreach (var styleItem in element.Styles)
        {
            string tailwindClassName;
            {
                var result = Css.ConvertDesignerStyleItemToTailwindClassName(styleItem);
                if (result.HasError)
                {
                    return result.Error;
                }

                tailwindClassName = result.Value;
            }

            if (tailwindClassName.StartsWith("${"))
            {
                classNameShouldBeTemplateLiteral = true;
            }

                
                
            if (styleItem.StartsWith("hover:"))
            {
                if (Project.Styles.TryGetValue(tailwindClassName, out var css))
                {
                    var result = Css.ConvertDesignerStyleItemToTailwindClassName(css);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    tailwindClassName = result.Value;
                }
                classNames.Add("hover:" + tailwindClassName);
                continue;
            }
                
            classNames.Add(tailwindClassName);
        }
        

        if (classNames.Any())
        {
            var firstLastChar = classNameShouldBeTemplateLiteral ? "`" : "\"";

            node.Properties.Add(new() { Name = "className", Value = firstLastChar + string.Join(" ", classNames) + firstLastChar });
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
            ReactNode childNode;
            {
                var result = await ConvertVisualElementModelToReactNodeModel(context, child);
                if (result.HasError)
                {
                    return result.Error;
                }

                childNode = result.Value;
            }

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
        public static Result<string> ConvertDesignerStyleItemToTailwindClassName(string designerStyleItem)
        {
            var parseResult = TryParsePropertyValue(designerStyleItem);
            if (parseResult.success)
            {
                return ConvertToTailwindClass(parseResult.name, parseResult.value);
            }

            return designerStyleItem;
        }

        static Result<string> ConvertToTailwindClass(string name, string value)
        {
            if (value is null)
            {
                return new ArgumentNullException(nameof(value));
            }

            // check is conditional sample: border-width: {props.isSelected} ? 2 : 5
            {
                var conditionalValue = TextParser.TryParseConditionalValue(value);
                if (conditionalValue.success)
                {
                    string lefTailwindClass;
                    {
                        var result = ConvertToTailwindClass(name, conditionalValue.left);
                        if (result.HasError)
                        {
                            return result.Error;
                        }

                        lefTailwindClass = result.Value;
                    }
                    
                    var rightTailwindClass = string.Empty;
                    
                    if (conditionalValue.right.HasValue())
                    {
                        {
                            var result = ConvertToTailwindClass(name, conditionalValue.right);
                            if (result.HasError)
                            {
                                return result.Error;
                            }

                            rightTailwindClass = result.Value;
                        }
                    }

                    return "${" + $"{ClearConnectedValue(conditionalValue.condition)} ? '{lefTailwindClass}' : '{rightTailwindClass}'" + '}';
                }
            }

            var isValueDouble = double.TryParse(value, out var valueAsDouble);

            name = name switch
            {
                "padding"  => "p",
                "padding-right"  => "pr",
                "padding-left"   => "pl",
                "padding-top"    => "pt",
                "padding-bottom" => "pb",
                
                "margin"  => "m",
                "margin-right"  => "mr",
                "margin-left"   => "ml",
                "margin-top"    => "mt",
                "margin-bottom" => "mb",

                _ => name
            };

            switch (name)
            {
                case "transform":
                {
                    if (value.StartsWith("rotate("))
                    {
                        var parts = value.Split("()".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length ==2)
                        {
                            var sign = parts[1][0]=='-' ? "-": "";
                            if (parts[1].EndsWith("deg"))
                            {
                                return $"{sign}rotate-{value.RemoveFromEnd("deg")}";
                            }
                        }
                        
                    }

                    break;
                }
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

               

                case "z-index":
                    return $"z-[{value}]";

                case "overflow-y":
                case "overflow-x":
                    return $"{name}-{value}";

               

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
                            _        => null
                        };

                        if (directionShortName is null)
                        {
                            return new ArgumentOutOfRangeException(direction);
                        }

                        return $"border-{directionShortName}-[{parts[0]}]" +
                               $" [border-{direction}-style:{parts[1]}]" +
                               $" [border-{direction}-color:{parts[2]}]";
                    }

                    return new ArgumentOutOfRangeException(direction);
                }

                

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

                case "border-color":
                {
                    if (Project.Colors.TryGetValue(value, out var htmlColor))
                    {
                        value = htmlColor;
                    }

                    return $"border-[{value}]";
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

                case "border-width":
                {
                    if (isValueDouble)
                    {
                        value = valueAsDouble.AsPixel();
                    }

                    return $"border-[{value}]";
                }

                
                case "m":
                case "mx":
                case "my":
                case "ml":
                case "mr":
                case "mb":
                case "mt":
                
                case "p":
                case "px":
                case "py":
                case "pl":
                case "pr":
                case "pb":
                case "pt":
                {
                    if (isValueDouble)
                    {
                        value = valueAsDouble.AsPixel();
                    }

                    return $"{name}-[{value}]";
                }
                
                
                
               

               
                
                
                

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
                            return "border " +
                                   $"border-[{parts[2]}]";
                        }

                        return $"border-[{parts[0]}] " +
                               $"border-[{parts[1]}] " +
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

                case "border-style":
                {
                    return $"border-{value}";
                }

                case "cursor":
                {
                    return $"cursor-{value}";
                }
                
                case "inset":
                {
                    return $"inset-{value}";
                }
            }

            return new InvalidOperationException($"Css not handled. {name}: {value}");
        }
    }

    static class TextParser
    {
        public static (bool success, string condition, string left, string right) TryParseConditionalValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (false, null, null, null);
            }

            // condition ? left : right  (right opsiyonel)
            var pattern = @"^\s*(?<condition>[^?]+?)\s*\?\s*(?<left>[^:]+?)\s*(?::\s*(?<right>.+))?$";
            var match = Regex.Match(value, pattern);

            if (match.Success)
            {
                var condition = match.Groups["condition"].Value.Trim();
                var left = match.Groups["left"].Value.Trim();
                var right = match.Groups["right"].Success ? match.Groups["right"].Value.Trim() : null;
                return (true, condition, left, right);
            }

            return (false, null, null, null);
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