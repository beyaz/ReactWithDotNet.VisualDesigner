using System.IO;
using System.Reflection;
using System.Text;

namespace ReactWithDotNet.VisualDesigner.Exporters;

sealed record ExportInput
{
    // @formatter:off
     
    public required int ProjectId { get; init; }
    
    public required int ComponentId { get; init; }
    
    public required string UserName { get; init; }
    
    public void Deconstruct(out int projectId, out int componentId,  out string userName)
    {
        projectId     = ProjectId;
        componentId     = ComponentId;
        
        userName      = UserName;
    }

    // @formatter:on
}

static class NextJs_with_Tailwind
{
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

    public static async Task<Result> ExportAll(int projectId)
    {
        var components = await GetAllComponentsInProject(projectId);

        foreach (var component in components)
        {
            if (component.Name == "/src/components/HggImage")
            {
                continue;
            }
            var result = await Export(new ()
            {
                ComponentId = component.Id,
                ProjectId = component.ProjectId,
                UserName = Environment.UserName
            });
            if (result.HasError)
            {
                return result.Error;
            }
        }

        return Success;
    }

    static async Task<Result<IReadOnlyList<string>>> CalculateElementTreeTsxCodes(int projectId, VisualElementModel rootVisualElement)
    {
        ReactNode rootNode;
        {
            var result = await ConvertVisualElementModelToReactNodeModel(projectId,rootVisualElement);
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
        var (projectId,componentId,  userName) = input;


        var user = GetUser(projectId, userName);

        var data = await GetComponentData(new() { ComponentId = componentId, UserName = userName });
        if (data.HasError)
        {
            return data.Error;
        }

        var componentName = data.Value.Component.Name;
        
        

        VisualElementModel rootVisualElement;
        {
            var result = await GetComponenUserOrMainVersionAsync(componentId, userName);
            if (result.HasError)
            {
                return result.Error;
            }

            rootVisualElement = result.Value;
        }
        
        

        string filePath, targetComponentName;
        {
            targetComponentName = componentName.Split('/').Last();
            
            filePath = Path.Combine((user.LocalWorkspacePath+ componentName + ".tsx").Split(new []{'/', Path.DirectorySeparatorChar}));

            if (Path.GetFileNameWithoutExtension(filePath).Contains("."))
            {
                var array = Path.GetFileName(filePath).Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (array.Length != 3)
                {
                    return new ArgumentException($"ComponentNameIsInvalid. {componentName}");
                }

                filePath = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, $"{array[0]}.{array[2]}");

                targetComponentName = array[1];
            }
        }


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
                var result = await CalculateElementTreeTsxCodes(projectId, rootVisualElement);
                if (result.HasError)
                {
                    return result.Error;
                }

                linesToInject = result.Value;
            }

            string injectedVersion;
            {
                var newVersion = InjectRender(fileContentInDirectory, targetComponentName, linesToInject);
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
                lines.Add($"{Indent(indentLevel)}{AsFinalText(node.Text)}");
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

        var elementType = TryGetHtmlElementTypeByTagName(nodeTag == "Image" ? "img" : (nodeTag == "Link" ? "a": nodeTag));
        
        var tag = nodeTag.Split(['/','.']).Last(); // todo: fix
        if (int.TryParse(nodeTag, out var componentId))
        {
            var component=DbOperation(db => db.FirstOrDefault<ComponentEntity>(x => x.Id == componentId));
            if (component is null)
            {
                return new ArgumentNullException($"ComponentNotFound. {componentId}");
            }

            tag = component.Name.Split(['/','.']).Last(); // todo: fix
        }

        var indent = new string(' ', indentLevel * 4);

        var sb = new StringBuilder();

        sb.Append($"{indent}<{tag}");

        var childrenProperty = node.Properties.FirstOrDefault(x => x.Name == "children");
        if (childrenProperty is not null)
        {
            node.Properties.Remove(childrenProperty);
        }

        var textProperty = node.Properties.FirstOrDefault(x => x.Name == Design.Text);
        if (textProperty is not null)
        {
            node.Properties.Remove(textProperty);
        }

        foreach (var reactProperty in node.Properties.Where(p=>p.Name.NotIn(Design.Text, Design.DesignText)))
        {
            var propertyName = reactProperty.Name;
            
            var propertyValue = reactProperty.Value;

            if (propertyName is "-items-source" || propertyName is "-items-source-design-time-count")
            {
                continue;
            }

            if (propertyValue == "false")
            {
                continue;
            }
            
            if (propertyValue == "true")
            {
                sb.Append($" {propertyName}");
                continue;
            }
            
            if (IsStringValue(propertyValue))
            {
                sb.Append($" {propertyName}={propertyValue}");
                continue;
            }

            if (IsConnectedValue(propertyValue))
            {
                sb.Append($" {propertyName}={propertyValue}");
                continue;
            }
            
            if (IsStringTemplate(propertyValue))
            {
                sb.Append($" {propertyName}={{{propertyValue}}}");
                continue;
            }
            
            if (elementType.HasValue)
            {
                var propertyType = elementType.Value.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.PropertyType;
                if (propertyType is not null)
                {
                    if (propertyType == typeof(string))
                    {
                        var isString = propertyValue.Contains("/") || propertyValue.StartsWith("#") || propertyValue.Split(' ').Length > 1;
                        if (isString)
                        {
                            sb.Append($" {propertyName}=\"{propertyValue}\"");
                            continue;
                        }
                    }

                    if ((propertyType == typeof(UnionProp<string,double?>) || propertyType == typeof(UnionProp<string,double>)) && double.TryParse(propertyValue, out _))
                    {
                        sb.Append($" {propertyName}={{{propertyValue}}}");
                        continue;
                    }
                }
            }
            
            sb.Append($" {propertyName}={{{propertyValue}}}");
        }

        var hasSelfClose = node.Children.Count == 0 && node.Text.HasNoValue() && childrenProperty is null;
        if (hasSelfClose)
        {
            sb.Append(" />");
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
                if (textProperty is not null)
                {
                    childrenText = AsFinalText(ClearConnectedValue(textProperty.Value));
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
            lines.Add($"{indent}{AsFinalText(node.Text)}");
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

    static string AsFinalText(string text)
    {
        if (!IsStringValue(text))
        {
            return $"{{{text}}}";    
        }
        
        return $"{{t(\"{TryClearStringValue(text)}\")}}";
    }

    static async Task<Result<ReactNode>> ConvertVisualElementModelToReactNodeModel(int projectId,VisualElementModel element)
    {
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
                element.Properties.Add("alt: \"?\"");
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
            var parseResult = TryParseProperty(property);
            if (parseResult.HasValue)
            {
                var (name, value) = parseResult.Value;

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
                
                node.Properties.Add(new() { Name = name, Value = value  });
            }
        }

        foreach (var styleItem in element.Styles)
        {
            string tailwindClassName;
            {
                var result = ConvertDesignerStyleItemToTailwindClassName(projectId,styleItem);
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

        var hasSelfClose = element.Children.Count == 0 && element.HasNoText();
        if (hasSelfClose)
        {
            return node;
        }
        
        // Add text content
        if (element.HasText())
        {
            node.Children.Add(new() { Text = element.GetText() });
        }

        // Add children
        foreach (var child in element.Children)
        {
            ReactNode childNode;
            {
                var result = await ConvertVisualElementModelToReactNodeModel(projectId, child);
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

    

    static string Indent(int indentLevel)
    {
        return new(' ', indentLevel * 4);
    }

    static Result<string> InjectRender(IReadOnlyList<string> fileContent, string targetComponentName, IReadOnlyList<string> linesToInject)
    {
        var lines = fileContent.ToList();
        
        // focus to component code

        var componentDeclerationLineIndex = lines.FindIndex(line=>line.Contains($"function {targetComponentName}("));
        if (componentDeclerationLineIndex == -1)
        {
            return new ArgumentException($"ComponentDeclerationNotFoundInFile. {targetComponentName}");
        }
        

        var firstReturnLineIndex = lines.FindIndex(componentDeclerationLineIndex, l => l == "    return (");
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