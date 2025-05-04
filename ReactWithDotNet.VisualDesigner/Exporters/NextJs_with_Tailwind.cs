using System.IO;
using System.Text;

namespace ReactWithDotNet.VisualDesigner.Exporters;

sealed record ExportInput
{
    // @formatter:off
     
    public required int ProjectId { get; init; }
    
    public required string ComponentName { get; init; }

    public required string UserName { get; init; }
    
    public void Deconstruct(out int projectId, out string componentName, out string userName)
    {
        projectId     = ProjectId;
        componentName = ComponentName;
        userName      = UserName;
    }

    // @formatter:on
}


static class NextJs_with_Tailwind
{
    public static async Task ExportAll(int projectId)
    {
        var components = await GetAllComponentsInProject(projectId);

        foreach (var component in components)
        {
            await Export(component.AsExportInput());
        }
    }
    
    public static async Task<Result> Export(ExportInput input)
    {
        var result = await CalculateExportInfo(input);
        if (result.HasError)
        {
            return result.Error;
        }

        var (filePath, fileContent) = result.Value;

        return await IO.TryWriteToFile(filePath, fileContent);
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

    static async Task<Result<(string filePath, string fileContent)>> CalculateExportInfo(ExportInput input)
    {
        var (projectId, componentName, userName) = input;

        ComponentEntity component;
        {
            var result = await GetComponenUserOrMainVersionAsync(projectId, componentName, userName);
            if (result.HasError)
            {
                return result.Error;
            }

            component = result.Value;
        }

        var filePath = $"{GetExportFolderPath()}{componentName}.tsx";

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
                var result = await CalculateElementTreeTsxCodes(component, userName);
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

                node.Properties.Add(new() { Name = name, Value = '"' + value + '"' });
            }
        }

        foreach (var styleItem in element.Styles)
        {
            string tailwindClassName;
            {
                var result = ConvertDesignerStyleItemToTailwindClassName(styleItem);
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