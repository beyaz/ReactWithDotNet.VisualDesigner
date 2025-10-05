using System.IO;
using System.Reflection;

namespace ReactWithDotNet.VisualDesigner.Exporters;

static class TsxExporter
{
    public static async Task<Result<string>> CalculateElementTsxCode(int projectId, IReadOnlyDictionary<string, string> componentConfig, VisualElementModel visualElement)
    {
        var project = GetProjectConfig(projectId);

        return
            from x in await CalculateElementTreeSourceCodes(project, componentConfig, visualElement)
            select string.Join(Environment.NewLine, x.elementTreeSourceLines);
    }

    public static  Task<Result<ExportOutput>> ExportToFileSystem(ExportInput input)
    {
        return 
            from file in CalculateExportInfo(input)
            from fileContentAtDisk in FileSystem.ReadAllText(file.filePath)
            select IsEqualsIgnoreWhitespace(fileContentAtDisk, file.fileContent) switch
            {
                true => Result.From(new ExportOutput()),
                false =>
                    from _ in FileSystem.TryWriteToFile(file.filePath, file.fileContent)
                    select new ExportOutput
                    {
                        HasChange = true
                    }
            };
    }

    public static Result<(int leftPaddingCount, int firstReturnLineIndex, int firstReturnCloseLineIndex)> GetComponentLineIndexPointsInTsxFile(IReadOnlyList<string> fileContent, string targetComponentName)
    {
        var lines = fileContent.ToList();

        var componentDeclarationLineIndex = lines.FindIndex(line => line.Contains($"function {targetComponentName}(", StringComparison.OrdinalIgnoreCase));
        if (componentDeclarationLineIndex < 0)
        {
            componentDeclarationLineIndex = lines.FindIndex(line => line.Contains($"const {targetComponentName} "));
            if (componentDeclarationLineIndex < 0)
            {
                componentDeclarationLineIndex = lines.FindIndex(line => line.Contains($"const {targetComponentName}:"));
                if (componentDeclarationLineIndex < 0)
                {
                    return new ArgumentException($"ComponentDeclarationNotFoundInFile. {targetComponentName}");
                }
            }
        }

        var leftPaddingCount = 0;
        var firstReturnLineIndex = -1;
        {
            for (var i = 1; i < 20; i++)
            {
                firstReturnLineIndex = lines.FindIndex(componentDeclarationLineIndex, l => l == new string(' ', i) + "return (");
                if (firstReturnLineIndex > 0)
                {
                    leftPaddingCount = i;
                    break;
                }
            }
        }

        var firstReturnCloseLineIndex = -1;
        {
            foreach (var item in lines.FindLineIndexStartsWith(firstReturnLineIndex, leftPaddingCount, ");"))
            {
                firstReturnCloseLineIndex = item;
            }

            if (firstReturnCloseLineIndex < 0)
            {
                return new ArgumentException($"ReturnClosePointNotFound. {targetComponentName}");
            }
        }

        return (leftPaddingCount, firstReturnLineIndex, firstReturnCloseLineIndex);
    }

    internal static async Task<Result<(IReadOnlyList<string> elementTreeSourceLines, IReadOnlyList<string> importLines)>> CalculateElementTreeSourceCodes(ProjectConfig project, IReadOnlyDictionary<string, string> componentConfig, VisualElementModel rootVisualElement)
    {
        ReactNode rootNode;
        {
            var result = await ModelToNodeTransformer.ConvertVisualElementModelToReactNodeModel(project, rootVisualElement);
            if (result.HasError)
            {
                return result.Error;
            }

            rootNode = result.Value;
        }

        rootNode = Plugin.AnalyzeNode(rootNode, componentConfig);

        IReadOnlyList<string> elementJsxTree;
        {
            var result = await ConvertReactNodeModelToElementTreeSourceLines(project, rootNode, null, 2);
            if (result.HasError)
            {
                return result.Error;
            }

            elementJsxTree = result.Value;
        }

        var importLines = Plugin.CalculateImportLines(rootNode);

        return (elementJsxTree, importLines.ToList());
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
            IReadOnlyList<string> fileContentInDirectory;
            {
                var result = await FileSystem.ReadAllLines(filePath);
                if (result.HasError)
                {
                    return result.Error;
                }

                fileContentInDirectory = result.Value;
            }

            IReadOnlyList<string> linesToInject;
            IReadOnlyList<string> importLines;
            {
                var result = await CalculateElementTreeSourceCodes(project, data.Value.Component.GetConfig(), rootVisualElement);
                if (result.HasError)
                {
                    return result.Error;
                }

                linesToInject = result.Value.elementTreeSourceLines;

                importLines = result.Value.importLines;

                var formatResult = await Prettier.FormatCode(string.Join(Environment.NewLine, linesToInject));
                if (!formatResult.HasError)
                {
                    linesToInject = formatResult.Value.Split(Environment.NewLine.ToCharArray());
                }
            }

            // try import lines
            {
                var fileContentInDirectoryAsList = fileContentInDirectory.ToList();

                foreach (var importLine in importLines)
                {
                    if (fileContentInDirectory.Any(x => IsEqualsIgnoreWhitespace(x, importLine)))
                    {
                        continue;
                    }

                    var lastImportLineIndex = fileContentInDirectoryAsList.FindLastIndex(line => line.TrimStart().StartsWith("import "));
                    if (lastImportLineIndex >= 0)
                    {
                        fileContentInDirectoryAsList.Insert(lastImportLineIndex + 1, importLine);
                    }
                }

                fileContentInDirectory = fileContentInDirectoryAsList;
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

    static async Task<Result<IReadOnlyList<string>>> ConvertReactNodeModelToElementTreeSourceLines(ProjectConfig project, ReactNode node, ReactNode parentNode, int indentLevel)
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
                    var result = await ConvertReactNodeModelToElementTreeSourceLines(project, node, parentNode, indentLevel);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    innerLines = result.Value;
                }

                // import inner lines
                // try clear begin - end brackets in innerLines 
                // maybe conditional render
                {
                    for (var i = 0; i < innerLines.Count; i++)
                    {
                        var line = innerLines[i];

                        // is first line
                        if (i == 0)
                        {
                            line = line.TrimStart().RemoveFromStart("{");
                        }

                        // is last line
                        if (i == innerLines.Count - 1)
                        {
                            line = line.TrimEnd().RemoveFromEnd("}");
                        }

                        lines.Add(line);
                    }
                }

                indentLevel--;
                lines.Add(indent(indentLevel) + ");");

                indentLevel--;
                lines.Add(indent(indentLevel) + "})}");

                return lines;
            }
        }

        // show hide
        {
            var showIf = node.Properties.FirstOrDefault(x => x.Name is Design.ShowIf);
            var hideIf = node.Properties.FirstOrDefault(x => x.Name is Design.HideIf);

            if (showIf is not null)
            {
                node = node with { Properties = node.Properties.Remove(showIf) };

                List<string> lines =
                [
                    $"{indent(indentLevel)}{{{showIf.Value} && ("
                ];

                indentLevel++;

                IReadOnlyList<string> innerLines;
                {
                    var result = await ConvertReactNodeModelToElementTreeSourceLines(project, node, parentNode, indentLevel);
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
                    $"{indent(indentLevel)}{{!{hideIf.Value} && ("
                ];
                indentLevel++;

                IReadOnlyList<string> innerLines;
                {
                    var result = await ConvertReactNodeModelToElementTreeSourceLines(project, node, parentNode, indentLevel);
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

            string partProps;
            {
                var propsAsTextList = new List<string>();
                {
                    foreach (var reactProperty in node.Properties.Where(p => p.Name.NotIn(Design.Text, Design.TextPreview, Design.Src, Design.Name)))
                    {
                        var text = convertReactPropertyToString(elementType, reactProperty);
                        if (text is not null)
                        {
                            propsAsTextList.Add(text);
                        }
                    }

                    static string convertReactPropertyToString(Maybe<Type> elementType, ReactProperty reactProperty)
                    {
                        var propertyName = reactProperty.Name;

                        var propertyValue = reactProperty.Value;

                        if (propertyName is Design.ItemsSource || propertyName is Design.ItemsSourceDesignTimeCount)
                        {
                            return null;
                        }

                        if (propertyValue == "true")
                        {
                            return propertyName;
                        }

                        if (propertyName == Design.SpreadOperator)
                        {
                            return '{' + propertyValue + '}';
                        }

                        if (propertyName == nameof(HtmlElement.dangerouslySetInnerHTML))
                        {
                            return $"{propertyName}={{{{ __html: {propertyValue} }}}}";
                        }

                        if (IsStringValue(propertyValue))
                        {
                            return $"{propertyName}=\"{TryClearStringValue(propertyValue)}\"";
                        }

                        if (IsStringTemplate(propertyValue))
                        {
                            return $"{propertyName}={{{propertyValue}}}";
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
                                        return $"{propertyName}=\"{propertyValue}\"";
                                    }
                                }
                            }
                        }

                        return $"{propertyName}={{{propertyValue}}}";
                    }
                }

                if (propsAsTextList.Count > 0)
                {
                    partProps = " " + string.Join(" ", propsAsTextList);
                }
                else
                {
                    partProps = string.Empty;
                }
            }

            if (node.Children.Count == 0 && node.Text.HasNoValue() && childrenProperty is null)
            {
                return new LineCollection
                {
                    $"{indent(indentLevel)}<{tag}{partProps} />"
                };
            }

            // children property
            {
                // sample: children: state.suggestionNodes

                if (childrenProperty is not null)
                {
                    return new LineCollection
                    {
                        $"{indent(indentLevel)}<{tag}{partProps}>",
                        $"{indent(indentLevel + 1)}{childrenProperty.Value}",
                        $"{indent(indentLevel)}</{tag}>"
                    };
                }
            }

            LineCollection lines =
            [
                $"{indent(indentLevel)}<{tag}{partProps}>"
            ];

            // Add children
            foreach (var child in node.Children)
            {
                IReadOnlyList<string> childTsx;
                {
                    var result = await ConvertReactNodeModelToElementTreeSourceLines(project, child, node, indentLevel + 1);
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
            const int IndentLength = 2;

            return new(' ', indentLevel * IndentLength);
        }
    }

    static Result<string> InjectRender(IReadOnlyList<string> fileContent, string targetComponentName, IReadOnlyList<string> linesToInject)
    {
        var lines = fileContent.ToList();

        // focus to component code
        int firstReturnLineIndex, firstReturnCloseLineIndex, leftPaddingCount;
        {
            var result = GetComponentLineIndexPointsInTsxFile(fileContent, targetComponentName);
            if (result.HasError)
            {
                return result.Error;
            }

            leftPaddingCount          = result.Value.leftPaddingCount;
            firstReturnLineIndex      = result.Value.firstReturnLineIndex;
            firstReturnCloseLineIndex = result.Value.firstReturnCloseLineIndex;
        }

        lines.RemoveRange(firstReturnLineIndex, firstReturnCloseLineIndex - firstReturnLineIndex + 1);

        // apply padding
        {
            var temp = linesToInject.Select(line => new string(' ', leftPaddingCount + 2) + line).ToList();

            temp.Insert(0, new string(' ', leftPaddingCount) + "return (");

            temp.Add(new string(' ', leftPaddingCount) + ");");

            linesToInject = temp;
        }

        lines.InsertRange(firstReturnLineIndex, linesToInject);

        var injectedFileContent = string.Join(Environment.NewLine, lines);

        return injectedFileContent;
    }

    class LineCollection : List<string>
    {
    }
}