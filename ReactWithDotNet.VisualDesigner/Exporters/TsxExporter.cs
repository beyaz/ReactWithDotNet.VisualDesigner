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

static class TsxExporter
{
    public static async Task<Result<string>> CalculateElementTsxCode(int projectId, VisualElementModel visualElement)
    {
        var project = GetProjectConfig(projectId);

        var result = await CalculateElementTreeTsxCodes(project, visualElement);
        if (result.HasError)
        {
            return result.Error;
        }

        return string.Join(Environment.NewLine, result.Value);
    }

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

        if (ignore_whitespace_characters(fileContentAtDisk) == ignore_whitespace_characters(fileContent))
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

        static string ignore_whitespace_characters(string value)
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

    internal static async Task<Result<IReadOnlyList<string>>> CalculateElementTreeTsxCodes(ProjectConfig project, VisualElementModel rootVisualElement)
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

        rootNode = arrangeImageAndLinkTags(rootNode);

        return await ConvertReactNodeModelToTsxCode(project,rootNode, null, 2);

        static ReactNode arrangeImageAndLinkTags(ReactNode node)
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
                Children = [..node.Children.Select(arrangeImageAndLinkTags)]
            };
        }
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
            var result = await GetComponentUserOrMainVersionAsync(componentId, userName);
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

    static async Task<Result<IReadOnlyList<string>>> ConvertReactNodeModelToTsxCode(ProjectConfig project, ReactNode node, ReactNode parentNode, int indentLevel)
    {
        var nodeTag = node.Tag;

        if (nodeTag == TextNode.Tag)
        {
            return new List<string>
            {
                $"{indent(indentLevel)}{asFinalText(project, node.Children[0].Text)}"
            };
        }

        if (nodeTag is null)
        {
            if (node.Text.HasValue())
            {
                return new List<string>
                {
                    $"{indent(indentLevel)}{asFinalText(project, node.Text)}"
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
                $"{indent(indentLevel)}{{{ClearConnectedValue(showIf.Value)} && ("
            ];

            indentLevel++;

            IReadOnlyList<string> innerLines;
            {
                var result = await ConvertReactNodeModelToTsxCode(project, node, parentNode, indentLevel);
                if (result.HasError)
                {
                    return result.Error;
                }

                innerLines = result.Value;
            }

            lines.AddRange(innerLines);

            indentLevel--;
            lines.Add($"{indent(indentLevel)})}}");

            return lines;
        }

        if (hideIf is not null)
        {
            node = node with { Properties = node.Properties.Remove(hideIf) };

            List<string> lines =
            [
                $"{indent(indentLevel)}{{!{ClearConnectedValue(hideIf.Value)} && ("
            ];
            indentLevel++;

            IReadOnlyList<string> innerLines;
            {
                var result = await ConvertReactNodeModelToTsxCode(project, node, parentNode, indentLevel);
                if (result.HasError)
                {
                    return result.Error;
                }

                innerLines = result.Value;
            }

            lines.AddRange(innerLines);

            indentLevel--;
            lines.Add($"{indent(indentLevel)})}}");

            return lines;
        }

        // is mapping
        {
            List<string> lines = [];

            var itemsSource = parentNode?.Properties.FirstOrDefault(x => x.Name is Design.ItemsSource);
            if (itemsSource is not null)
            {
                parentNode = parentNode with { Properties = parentNode.Properties.Remove(itemsSource) };

                lines.Add($"{indent(indentLevel)}{{{ClearConnectedValue(itemsSource.Value)}.map((_item, _index) => {{");
                indentLevel++;

                lines.Add(indent(indentLevel) + "return (");
                indentLevel++;

                IReadOnlyList<string> innerLines;
                {
                    var result = await ConvertReactNodeModelToTsxCode(project, node, parentNode, indentLevel);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    innerLines = result.Value;
                }
                lines.AddRange(innerLines);

                indentLevel--;
                lines.Add(indent(indentLevel) + ");");

                indentLevel--;
                lines.Add(indent(indentLevel) + "})}");

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
                            $"{indent(indentLevel)}<{tag} {string.Join(" ", propsAsText)} />"
                        };
                    }

                    return new TsxLines
                    {
                        $"{indent(indentLevel)}<{tag}",
                        propsAsMultiLines(propsAsText, indentLevel),
                        $"{indent(indentLevel)}/>"
                    };
                }

                return new TsxLines
                {
                    $"{indent(indentLevel)}<{tag} />"
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
                                $"{indent(indentLevel)}<{tag} {string.Join(" ", propsAsText)}>",
                                $"{indent(indentLevel + 1)}{childrenProperty.Value}",
                                $"{indent(indentLevel)}</{tag}>"
                            };
                        }

                        return new TsxLines
                        {
                            $"{indent(indentLevel)}<{tag}",
                            propsAsMultiLines(propsAsText, indentLevel),
                            $"{indent(indentLevel)}>",

                            $"{indent(indentLevel + 1)}{childrenProperty.Value}",
                            $"{indent(indentLevel)}</{tag}>"
                        };
                    }

                    return new TsxLines
                    {
                        $"{indent(indentLevel)}<{tag}>",
                        $"{indent(indentLevel + 1)}{childrenProperty.Value}",
                        $"{indent(indentLevel)}</{tag}>"
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
                        childrenText = asFinalText(project, ClearConnectedValue(textProperty.Value));
                    }

                    if (IsConnectedValue(childrenText))
                    {
                        if (propsAsText.Count > 0)
                        {
                            if (propsCanExportInOneLine)
                            {
                                return new TsxLines
                                {
                                    $"{indent(indentLevel)}<{tag} {string.Join(" ", propsAsText)}>",
                                    $"{indent(indentLevel + 1)}{childrenText}",
                                    $"{indent(indentLevel)}</{tag}>"
                                };
                            }

                            return new TsxLines
                            {
                                $"{indent(indentLevel)}<{tag}",
                                propsAsMultiLines(propsAsText, indentLevel),
                                $"{indent(indentLevel)}>",

                                $"{indent(indentLevel + 1)}{childrenText}",
                                $"{indent(indentLevel)}</{tag}>"
                            };
                        }

                        return new TsxLines
                        {
                            $"{indent(indentLevel)}<{tag}>",
                            $"{indent(indentLevel + 1)}{childrenText}",
                            $"{indent(indentLevel)}</{tag}>"
                        };
                    }
                }
            }

            TsxLines lines = [];

            if (propsAsText.Count > 0)
            {
                if (propsCanExportInOneLine)
                {
                    lines.Add($"{indent(indentLevel)}<{tag} {string.Join(" ", propsAsText)}>");
                }
                else
                {
                    lines.Add($"{indent(indentLevel)}<{tag}");

                    lines.AddRange(propsAsMultiLines(propsAsText, indentLevel));

                    lines.Add($"{indent(indentLevel)}>");
                }
            }
            else
            {
                lines.Add($"{indent(indentLevel)}<{tag}>");
            }

            // Add children
            foreach (var child in node.Children)
            {
                IReadOnlyList<string> childTsx;
                {
                    var result = await ConvertReactNodeModelToTsxCode(project, child, node, indentLevel + 1);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    childTsx = result.Value;
                }

                lines.AddRange(childTsx);
            }

            // Close tag
            lines.Add($"{indent(indentLevel)}</{tag}>");

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

                        lines.Add(indent(indentLevel + 1) + "className={`");

                        lines.AddRange(items.Select(x => indent(indentLevel + 2) + x));
                        lines.Add(indent(indentLevel + 1) + "`}");
                        continue;
                    }
                }

                lines.Add(indent(indentLevel + 1) + propItem);
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

        static string asFinalText(ProjectConfig project, string text)
        {
            if (IsRawStringValue(text))
            {
                return $"{TryClearRawStringValue(text)}";
            }
            
            if (!IsStringValue(text))
            {
                return $"{{{text}}}";
            }

            // try to export with translation function
            {
                var translateFunction = project.TranslationFunctionName;
                {
                    if (translateFunction.HasValue())
                    {
                        return $"{{{translateFunction.Trim()}(\"{TryClearStringValue(text)}\")}}";
                    }
                }
            }

            return "{" + '"' + TryClearStringValue(text) + '"' + "}";
        }

        static string indent(int indentLevel)
        {
            return new(' ', indentLevel * ESLint.IndentLength);
        }
    }

    static Result<(VisualElementModel modifiedElementModel, IReadOnlyList<(string name, string value)> inlineStyle)>
        convertStyleToInlineStyleObject(VisualElementModel elementModel)
    {
        var inlineStyles = new List<(string name, string value)>();

        foreach (var item in elementModel.Styles)
        {
            var styleAttribute = ParseStyleAttribute(item);

            inlineStyles.Add((styleAttribute.Name, styleAttribute.Value));
        }

        elementModel = elementModel with { Styles = [] };

        return (elementModel, inlineStyles);
    }

    static async Task<Result<ReactNode>> ConvertVisualElementModelToReactNodeModel(ProjectConfig project, VisualElementModel elementModel)
    {
        // Open tag
        var tag = elementModel.Tag;

        if (tag == "img")
        {
            if (!elementModel.Properties.Any(x => x.Contains("alt:")))
            {
                elementModel = elementModel with { Properties = elementModel.Properties.Add("alt: \"?\"") };
            }

            var sizeProperty = elementModel.Properties.FirstOrDefault(x => x.Contains("size:"));
            if (sizeProperty is not null)
            {
                elementModel = elementModel with { Properties = elementModel.Properties.Remove(sizeProperty) };

                elementModel = elementModel with { Properties = elementModel.Properties.Add(sizeProperty.Replace("size:", "width:")) };

                elementModel = elementModel with { Properties = elementModel.Properties.Add(sizeProperty.Replace("size:", "height:")) };
            }

            // try to add width and height to default style
            {
                // width
                {
                    foreach (var propertyValue in elementModel.Properties.TryGetPropertyValue("width", "w"))
                    {
                        if (elementModel.Styles.TryGetPropertyValue("width", "w").HasNoValue)
                        {
                            elementModel = elementModel with { Styles = elementModel.Styles.Add($"width: {propertyValue}") };
                        }
                    }
                }

                // height
                {
                    foreach (var propertyValue in elementModel.Properties.TryGetPropertyValue("height", "h"))
                    {
                        if (elementModel.Styles.TryGetPropertyValue("height", "h").HasNoValue)
                        {
                            elementModel = elementModel with { Styles = elementModel.Styles.Add($"height: {propertyValue}") };
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

        // arrange inline styles
        {
            if (project.ExportStylesAsInline)
            {
                var result = convertStyleToInlineStyleObject(elementModel);
                if (result.HasError)
                {
                    return result.Error;
                }

                var modifiedElementModel = result.Value.modifiedElementModel;
                var inlineStyle = result.Value.inlineStyle;

                elementModel = modifiedElementModel;
                if (inlineStyle.Any())
                {
                    // todo: fix et {{
                    var inlineStyleProperty = new ReactProperty
                    {
                        Name  = "style",
                        Value = "{" + string.Join(", ", inlineStyle.Select(x => $"{x.name}: {x.value}")) + "}"
                    };

                    node = node with { Properties = node.Properties.Add(inlineStyleProperty) };
                }
            }
        }

        // arrange tailwind classes
        {
            if (project.ExportStylesAsTailwind)
            {
                List<string> classNames = [];

                var classNameShouldBeTemplateLiteral = false;

                // Add properties
                foreach (var property in elementModel.Properties)
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

                foreach (var styleItem in elementModel.Styles)
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
            }
        }

        var hasNoChildAndHasNoText = elementModel.Children.Count == 0 && elementModel.HasNoText();
        if (hasNoChildAndHasNoText)
        {
            return node;
        }

        // Add text content
        if (elementModel.HasText())
        {
            node = node with
            {
                Children = node.Children.Add(new()
                {
                    Text = elementModel.GetText(),

                    HtmlElementType = None
                })
            };
        }

        // Add children
        foreach (var child in elementModel.Children)
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
            var parseResult = TryParseProperty(property);
            if (parseResult.HasNoValue)
            {
                return (None, None);
            }

            var (name, value) = parseResult.Value;

            if (name == "class")
            {
                return (None, value.Split(" ", StringSplitOptions.RemoveEmptyEntries));
            }

            if (name is "w" or "width")
            {
                return (new ReactProperty { Name = "width", Value = value }, None);
            }

            if (name is "h" or "height")
            {
                return (new ReactProperty { Name = "height", Value = value }, None);
            }

            return (new ReactProperty { Name = name, Value = value }, None);
        }
    }

    static Result<string> InjectRender(IReadOnlyList<string> fileContent, string targetComponentName, IReadOnlyList<string> linesToInject)
    {
        var lines = fileContent.ToList();

        // focus to component code
        int componentDeclarationLineIndex;
        {
            var result = GetComponentDeclarationLineIndex(fileContent, targetComponentName);
            if (result.HasError)
            {
                return result.Error;
            }

            componentDeclarationLineIndex = result.Value;
        }

        var firstReturnLineIndex = lines.FindIndex(componentDeclarationLineIndex, l => l == "    return (");
        if (firstReturnLineIndex < 0)
        {
            firstReturnLineIndex = lines.FindIndex(componentDeclarationLineIndex, l => l == "  return (");
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

        public ImmutableList<ReactProperty> Properties { get; init; } = [];

        public string Tag { get; init; }

        public string Text { get; init; }

        internal required Maybe<Type> HtmlElementType { get; init; }
    }

    record ReactProperty
    {
        public required string Name { get; init; }
        public required string Value { get; init; }
    }
}