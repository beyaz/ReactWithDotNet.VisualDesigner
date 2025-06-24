using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ReactWithDotNet.VisualDesigner.Exporters;

sealed record ExportOutput
{
    public bool HasChange { get; init; }
}

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

static class ESLint
{
    public static readonly int IndentLength = 2;
    public static readonly int MaxCharLengthPerLine = 80;
}

static class NextJs_with_Tailwind
{
    public static async Task<Result<ExportOutput>> Export(ExportInput input)
    {
        string filePath;
        string fileContent;
        {
            var result = await CalculateExportInfo(input);
            if (result.HasError)
            {
                return result.Error;
            }

            (filePath, fileContent) = result.Value;
        }

        string fileContentAtDisk;
        {
            var result = await IO.TryReadFile(filePath);
            if (result.HasError)
            {
                return result.Error;
            }

            fileContentAtDisk = result.Value;
        }

        if (ignore_whitespace_charachters(fileContentAtDisk) == ignore_whitespace_charachters(fileContent))
        {
            return new ExportOutput();
        }

        // write to file system
        {
            var result = await IO.TryWriteToFile(filePath, fileContent);
            if (result.HasError)
            {
                return result.Error;
            }
        }

        return new ExportOutput { HasChange = true };
        
        static string ignore_whitespace_charachters(string value)
        {
            if (value == null)
            {
                return null;
            }

            return Regex.Replace(value, @"\s+", string.Empty);
        }
    }

    public static async Task<Result> ExportAll(int projectId)
    {
        var components = await Store.GetAllComponentsInProject(projectId);

        foreach (var component in components)
        {
            if (component.GetConfig().TryGetValue("IsExportable", out var isExportable) && isExportable.Equals("False", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var result = await Export(new()
            {
                ComponentId = component.Id,
                ProjectId   = component.ProjectId,
                UserName    = Environment.UserName
            });
            if (result.HasError)
            {
                return result.Error;
            }
        }

        return Success;
    }

    static ReactNode ArrangeImageAndLinkTags(ReactNode node)
    {
        if (node.Tag == "img")
        {
            node = node with { Tag = "Image" };
            if (!(node.Properties.Any(x => x.Name == "width") && node.Properties.Any(x => x.Name == "height")))
            {
                node = node with { Properties = node.Properties.Add(new() { Name = "fill", Value = "true" }) };
            }
        }

        if (node.Tag == "a")
        {
            node = node with { Tag = "Link" };
        }

        return node with
        {
            Children = [..node.Children.Select(ArrangeImageAndLinkTags)]
        };
    }

    static string AsFinalText(string text)
    {
        if (!IsStringValue(text))
        {

            if (IsRawStringValue(text))
            {
                return $"{TryClearRawStringValue(text)}";
            }


            return $"{{{text}}}";
        }

        return $"{{t(\"{TryClearStringValue(text)}\")}}";
    }

    static async Task<Result<IReadOnlyList<string>>> CalculateElementTreeTsxCodes(ProjectConfig project, VisualElementModel rootVisualElement)
    {
        ReactNode rootNode;
        {
            var result = await ConvertVisualElementModelToReactNodeModel(project, rootVisualElement);
            if (result.HasError)
            {
                return result.Error;
            }

            rootNode = result.Value;
        }

        rootNode = ArrangeImageAndLinkTags(rootNode);

        return await ConvertReactNodeModelToTsxCode(rootNode, null, 2);
    }

    static async Task<Result<(string filePath, string fileContent)>> CalculateExportInfo(ExportInput input)
    {
        var (projectId, componentId, userName) = input;

        var user = await Store.TryGetUser(projectId, userName);

        var project = GetProjectConfig(projectId);

        var data = await GetComponentData(new() { ComponentId = componentId, UserName = userName });
        if (data.HasError)
        {
            return data.Error;
        }

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
            var result = await GetComponentFileLocation(componentId, user.LocalWorkspacePath);
            if (result.HasError)
            {
                return result;
            }

            filePath            = result.Value.filePath;
            targetComponentName = result.Value.targetComponentName;
        }

       
        
        // create File if not exists
        {
            if (filePath is null)
            {
                return new IOException("FilePathNotCalculated");
            }
            
            if (!File.Exists(filePath))
            {
                
                var fileContent = 
                    $"export default function {targetComponentName}()" + "{" + Environment.NewLine +
                    "    return (" + Environment.NewLine +
                    "    );" + Environment.NewLine +
                    "}";
                await File.WriteAllTextAsync(filePath, fileContent);
            }
        }
        
        
        string fileNewContent;
        {
            string[] fileContentInDirectory;
            {
                var result = await IO.TryReadFileAllLines(filePath);
                if (result.HasError)
                {
                    return result.Error;
                }

                fileContentInDirectory = result.Value;
            }

            IReadOnlyList<string> linesToInject;
            {
                var result = await CalculateElementTreeTsxCodes(project, rootVisualElement);
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

    static async Task<Result<IReadOnlyList<string>>> ConvertReactNodeModelToTsxCode(ReactNode node, ReactNode parentNode, int indentLevel)
    {
        var nodeTag = node.Tag;

        if (nodeTag == TextNode.Tag)
        {
            return new List<string>
            {
                $"{Indent(indentLevel)}{AsFinalText(node.Children[0].Text)}"
            };
        }
        
        if (nodeTag is null)
        {
            if (node.Text.HasValue())
            {
                return new List<string>
                {
                    $"{Indent(indentLevel)}{AsFinalText(node.Text)}"
                };
            }

            return new ArgumentNullException(nameof(nodeTag));
        }

        var showIf = node.Properties.FirstOrDefault(x => x.Name is Design.ShowIf);
        var hideIf = node.Properties.FirstOrDefault(x => x.Name is Design.HideIf);

        if (showIf is not null)
        {
            node = node with { Properties = node.Properties.Remove(showIf) };

            List<string> lines =
            [
                $"{Indent(indentLevel)}{{{ClearConnectedValue(showIf.Value)} && ("
            ];

            indentLevel++;

            IReadOnlyList<string> innerLines;
            {
                var result = await ConvertReactNodeModelToTsxCode(node, parentNode, indentLevel);
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
            node = node with { Properties = node.Properties.Remove(hideIf) };

            List<string> lines =
            [
                $"{Indent(indentLevel)}{{!{ClearConnectedValue(hideIf.Value)} && ("
            ];
            indentLevel++;

            IReadOnlyList<string> innerLines;
            {
                var result = await ConvertReactNodeModelToTsxCode(node, parentNode, indentLevel);
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
            List<string> lines = [];

            var itemsSource = parentNode?.Properties.FirstOrDefault(x => x.Name is Design.ItemsSource);
            if (itemsSource is not null)
            {
                parentNode = parentNode with { Properties = parentNode.Properties.Remove(itemsSource) };

                lines.Add($"{Indent(indentLevel)}{{{ClearConnectedValue(itemsSource.Value)}.map((_item, _index) => {{");
                indentLevel++;

                lines.Add(Indent(indentLevel) + "return (");
                indentLevel++;

                IReadOnlyList<string> innerLines;
                {
                    var result = await ConvertReactNodeModelToTsxCode(node, parentNode, indentLevel);
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
                lines.Add(Indent(indentLevel) + "})}");

                return lines;
            }
        }

        {
            var elementType = node.HtmlElementType;

            var tag = nodeTag;
            if (int.TryParse(nodeTag, out var componentId))
            {
                var component = await Store.TryGetComponent(componentId);
                if (component is null)
                {
                    return new ArgumentNullException($"ComponentNotFound. {componentId}");
                }

                tag = component.GetName();
            }

            var childrenProperty = node.Properties.FirstOrDefault(x => x.Name == "children");
            if (childrenProperty is not null)
            {
                node = node with { Properties = node.Properties.Remove(childrenProperty) };
            }

            var textProperty = node.Properties.FirstOrDefault(x => x.Name == Design.Text);
            if (textProperty is not null)
            {
                node = node with { Properties = node.Properties.Remove(textProperty) };
            }

            var propsAsText = new List<string>();

            foreach (var reactProperty in node.Properties.Where(p => p.Name.NotIn(Design.Text, Design.TextPreview, Design.Src)))
            {
                var propertyName = reactProperty.Name;

                var propertyValue = reactProperty.Value;

                if (propertyName is Design.ItemsSource || propertyName is Design.ItemsSourceDesignTimeCount)
                {
                    continue;
                }

                if (propertyValue == "false")
                {
                    continue;
                }

                if (propertyValue == "true")
                {
                    propsAsText.Add($"{propertyName}");
                    continue;
                }

                if (propertyName == Design.SpreadOperator)
                {
                    propsAsText.Add($"{{{propertyValue}}}");
                    continue;
                }

                if (propertyName == nameof(HtmlElement.dangerouslySetInnerHTML))
                {
                    propsAsText.Add($"{propertyName}={{{{ __html: {propertyValue} }}}}");
                    continue;
                }

                if (IsStringValue(propertyValue))
                {
                    propsAsText.Add($"{propertyName}=\"{TryClearStringValue(propertyValue)}\"");
                    continue;
                }

                if (IsConnectedValue(propertyValue))
                {
                    propsAsText.Add($"{propertyName}={propertyValue}");
                    continue;
                }

                if (IsStringTemplate(propertyValue))
                {
                    propsAsText.Add($"{propertyName}={{{propertyValue}}}");
                    continue;
                }

                if (elementType.HasValue)
                {
                    var propertyType = elementType.Value.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.PropertyType;
                    if (propertyType is not null)
                    {
                        if (propertyType == typeof(string))
                        {
                            var isString = propertyValue.Contains('/') || propertyValue.StartsWith('#') || propertyValue.Split(' ').Length > 1;
                            if (isString)
                            {
                                propsAsText.Add($"{propertyName}=\"{propertyValue}\"");
                                continue;
                            }
                        }

                        if ((propertyType == typeof(UnionProp<string, double?>) || propertyType == typeof(UnionProp<string, double>)) && double.TryParse(propertyValue, out _))
                        {
                            propsAsText.Add($"{propertyName}={{{propertyValue}}}");
                            continue;
                        }
                    }
                }

                propsAsText.Add($"{propertyName}={{{propertyValue}}}");
            }

            var propsCanExportInOneLine =
                propsAsText.Count == 0 || propsAsText.Count == 1 ||
                tag.Length + " ".Length + string.Join(" ", propsAsText).Length <= ESLint.MaxCharLengthPerLine;

            if (node.Children.Count == 0 && node.Text.HasNoValue() && childrenProperty is null)
            {
                if (propsAsText.Count > 0)
                {
                    if (propsCanExportInOneLine)
                    {
                        return new TsxLines
                        {
                            $"{Indent(indentLevel)}<{tag} {string.Join(" ", propsAsText)} />"
                        };
                    }

                    return new TsxLines
                    {
                        $"{Indent(indentLevel)}<{tag}",
                        propsAsMultiLines(propsAsText, indentLevel),
                        $"{Indent(indentLevel)}/>"
                    };
                }

                return new TsxLines
                {
                    $"{Indent(indentLevel)}<{tag} />"
                };
            }

            // children property
            {
                // sample: children: state.suggestionNodes

                if (childrenProperty is not null)
                {
                    if (propsAsText.Count > 0)
                    {
                        if (propsCanExportInOneLine)
                        {
                            return new TsxLines
                            {
                                $"{Indent(indentLevel)}<{tag} {string.Join(" ", propsAsText)}>",
                                $"{Indent(indentLevel + 1)}{childrenProperty.Value}",
                                $"{Indent(indentLevel)}</{tag}>"
                            };
                        }

                        return new TsxLines
                        {
                            $"{Indent(indentLevel)}<{tag}",
                            propsAsMultiLines(propsAsText, indentLevel),
                            $"{Indent(indentLevel)}>",

                            $"{Indent(indentLevel + 1)}{childrenProperty.Value}",
                            $"{Indent(indentLevel)}</{tag}>"
                        };
                    }

                    return new TsxLines
                    {
                        $"{Indent(indentLevel)}<{tag}>",
                        $"{Indent(indentLevel + 1)}{childrenProperty.Value}",
                        $"{Indent(indentLevel)}</{tag}>"
                    };
                }
            }

            // inner text
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
                        if (propsAsText.Count > 0)
                        {
                            if (propsCanExportInOneLine)
                            {
                                return new TsxLines
                                {
                                    $"{Indent(indentLevel)}<{tag} {string.Join(" ", propsAsText)}>",
                                    $"{Indent(indentLevel + 1)}{childrenText}",
                                    $"{Indent(indentLevel)}</{tag}>"
                                };
                            }

                            return new TsxLines
                            {
                                $"{Indent(indentLevel)}<{tag}",
                                propsAsMultiLines(propsAsText, indentLevel),
                                $"{Indent(indentLevel)}>",

                                $"{Indent(indentLevel + 1)}{childrenText}",
                                $"{Indent(indentLevel)}</{tag}>"
                            };
                        }

                        return new TsxLines
                        {
                            $"{Indent(indentLevel)}<{tag}>",
                            $"{Indent(indentLevel + 1)}{childrenText}",
                            $"{Indent(indentLevel)}</{tag}>"
                        };
                    }
                }
            }

            TsxLines lines = [];

            if (propsAsText.Count > 0)
            {
                if (propsCanExportInOneLine)
                {
                    lines.Add($"{Indent(indentLevel)}<{tag} {string.Join(" ", propsAsText)}>");
                }
                else
                {
                    lines.Add($"{Indent(indentLevel)}<{tag}");

                    lines.AddRange(propsAsMultiLines(propsAsText, indentLevel));

                    lines.Add($"{Indent(indentLevel)}>");
                }
            }
            else
            {
                lines.Add($"{Indent(indentLevel)}<{tag}>");
            }

            // Add children
            foreach (var child in node.Children)
            {
                IReadOnlyList<string> childTsx;
                {
                    var result = await ConvertReactNodeModelToTsxCode(child, node, indentLevel + 1);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    childTsx = result.Value;
                }

                lines.AddRange(childTsx);
            }

            // Close tag
            lines.Add($"{Indent(indentLevel)}</{tag}>");

            return lines;
        }

        static IReadOnlyList<string> propsAsMultiLines(IReadOnlyList<string> propsAsText, int indentLevel)
        {
            List<string> lines = [];

            foreach (var propItem in propsAsText)
            {
                if (propItem.StartsWith("className", StringComparison.OrdinalIgnoreCase))
                {
                    if (propItem.Contains("`"))
                    {
                        var items = splitClassName(propItem.RemoveFromStart("className={`").RemoveFromEnd("`}"));

                        lines.Add(Indent(indentLevel + 1) + "className={`");

                        lines.AddRange(items.Select(x => Indent(indentLevel + 2) + x));
                        lines.Add(Indent(indentLevel + 1) + "`}");
                        continue;
                    }
                }

                lines.Add(Indent(indentLevel + 1) + propItem);
            }

            return lines;

            static List<string> splitClassName(string input)
            {
                var result = new List<string>();

                // ${...} patternini yakala
                var pattern = @"(\$\{.*?\})";
                var parts = Regex.Split(input, pattern);

                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        continue;
                    }

                    result.Add(trimmed);
                }

                return result;
            }
        }
    }

    static async Task<Result<ReactNode>> ConvertVisualElementModelToReactNodeModel(ProjectConfig project, VisualElementModel element)
    {
        List<string> classNames = [];

        var classNameShouldBeTemplateLiteral = false;

        // Open tag
        var tag = element.Tag;

        if (tag == "img")
        {
            if (!element.Properties.Any(x => x.Contains("alt:")))
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

            // try to add width and height to default style
            {
                // width
                {
                    foreach (var propertyValue in element.Properties.TryGetPropertyValue("width", "w"))
                    {
                        if (element.Styles.TryGetPropertyValue("width", "w").HasNoValue)
                        {
                            element.Styles.Add($"width: {propertyValue}");
                        }
                    }
                }

                // height
                {
                    foreach (var propertyValue in element.Properties.TryGetPropertyValue("height", "h"))
                    {
                        if (element.Styles.TryGetPropertyValue("height", "h").HasNoValue)
                        {
                            element.Styles.Add($"height: {propertyValue}");
                        }
                    }
                }
            }
        }

        var node = new ReactNode
        {
            Tag = tag,

            HtmlElementType = TryGetHtmlElementTypeByTagName(tag)
        };

        // Add properties
        foreach (var property in element.Properties)
        {
            var (reactProperty, classNameList) = tryConvertToReactProperty(property);
            if (classNameList.HasValue)
            {
                classNames.AddRange(classNameList.Value);
                continue;
            }

            if (reactProperty.HasValue)
            {
                node = node with { Properties = node.Properties.Add(reactProperty.Value) };
                continue;
            }

            return new Exception($"PropertyParseError: {property}");
        }

        foreach (var styleItem in element.Styles)
        {
            string tailwindClassName;
            {
                var result = ConvertDesignerStyleItemToTailwindClassName(project, styleItem);
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

        if (classNames.Count > 0)
        {
            var firstLastChar = classNameShouldBeTemplateLiteral ? "`" : "\"";

            node = node with { Properties = node.Properties.Add(new() { Name = "className", Value = firstLastChar + string.Join(" ", classNames) + firstLastChar }) };
        }

        var hasSelfClose = element.Children.Count == 0 && element.HasNoText();
        if (hasSelfClose)
        {
            return node;
        }

        // Add text content
        if (element.HasText())
        {
            node = node with
            {
                Children = node.Children.Add(new()
                {
                    Text            = element.GetText(),
                    HtmlElementType = None
                })
            };
        }

        // Add children
        foreach (var child in element.Children)
        {
            ReactNode childNode;
            {
                var result = await ConvertVisualElementModelToReactNodeModel(project, child);
                if (result.HasError)
                {
                    return result.Error;
                }

                childNode = result.Value;
            }

            node = node with
            {
                Children = node.Children.Add(childNode)
            };
        }

        return node;

        static (Maybe<ReactProperty> reactProperty, Maybe<IReadOnlyList<string>> classNames) tryConvertToReactProperty(string property)
        {
            Maybe<ReactProperty> reactProperty = None;
            Maybe<IReadOnlyList<string>> classNames = None;

            var parseResult = TryParseProperty(property);
            if (parseResult.HasNoValue)
            {
                return (reactProperty, classNames);
            }

            var (name, value) = parseResult.Value;

            if (name == "class")
            {
                classNames = value.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                return (reactProperty, classNames);
            }

            if (name == "w" || name == "width")
            {
                reactProperty = new ReactProperty { Name = "width", Value = value };
                return (reactProperty, classNames);
            }

            if (name == "h" || name == "height")
            {
                reactProperty = new ReactProperty { Name = "height", Value = value };
                return (reactProperty, classNames);
            }

            reactProperty = new ReactProperty { Name = name, Value = value };

            return (reactProperty, classNames);
        }
    }

    static string Indent(int indentLevel)
    {
        return new(' ', indentLevel * ESLint.IndentLength);
    }

    static Result<string> InjectRender(IReadOnlyList<string> fileContent, string targetComponentName, IReadOnlyList<string> linesToInject)
    {
        var lines = fileContent.ToList();

        // focus to component code
        int componentDeclerationLineIndex;
        {
            var result = GetComponentDeclerationLineIndex(fileContent, targetComponentName);
            if (result.HasError)
            {
                return result.Error;
            }

            componentDeclerationLineIndex = result.Value;
        }

        var firstReturnLineIndex = lines.FindIndex(componentDeclerationLineIndex, l => l == "    return (");
        if (firstReturnLineIndex < 0)
        {
            firstReturnLineIndex = lines.FindIndex(componentDeclerationLineIndex, l => l == "  return (");
            if (firstReturnLineIndex < 0)
            {
                return new InvalidOperationException("No return found");
            }
        }

        var firstReturnCloseLineIndex = lines.FindIndex(firstReturnLineIndex, l => l == "    );");
        if (firstReturnCloseLineIndex < 0)
        {
            firstReturnCloseLineIndex = lines.FindIndex(firstReturnLineIndex, l => l == "  );");
            if (firstReturnCloseLineIndex < 0)
            {
                return new InvalidOperationException("Return close not found");
            }
        }

        lines.RemoveRange(firstReturnLineIndex + 1, firstReturnCloseLineIndex - firstReturnLineIndex - 1);

        lines.InsertRange(firstReturnLineIndex + 1, linesToInject);

        var injectedFileContent = string.Join(Environment.NewLine, lines);

        return injectedFileContent;
    }

    class TsxLines : List<string>
    {
        public void Add(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                Add(line);
            }
        }
    }

    record ReactNode
    {
        public ImmutableList<ReactNode> Children { get; init; } = [];

        public required Maybe<Type> HtmlElementType { get; init; }

        public ImmutableList<ReactProperty> Properties { get; init; } = [];

        public string Tag { get; init; }

        public string Text { get; init; }
    }

    record ReactProperty
    {
        public required string Name { get; init; }
        public required string Value { get; init; }
    }
}