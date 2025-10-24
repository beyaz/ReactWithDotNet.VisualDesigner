using System.Collections.Immutable;
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

    public static Task<Result<ExportOutput>> ExportToFileSystem(ExportInput input)
    {
        return
            from file in CalculateExportInfo(input)
            from fileContentAtDisk in FileSystem.ReadAllText(file.Path)
            select IsEqualsIgnoreWhitespace(fileContentAtDisk, file.Content) switch
            {
                true => Result.From(new ExportOutput()),
                false =>
                    from _ in FileSystem.Save(file)
                    select new ExportOutput
                    {
                        HasChange = true
                    }
            };
    }

    public static Result<SourceLinePoints> GetComponentLineIndexPointsInTsxFile(IReadOnlyList<string> fileContent, string targetComponentName)
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
            var leftSpaceCount = Array.FindIndex(lines[componentDeclarationLineIndex].ToCharArray(), c => c != ' ');

            foreach (var tabLength in new[] { 4, 2 })
            {
                foreach (var item in lines.FindLineIndexEquals(componentDeclarationLineIndex, leftSpaceCount + tabLength, "return ("))
                {
                    firstReturnLineIndex = item;
                    leftPaddingCount     = leftSpaceCount + tabLength;
                }

                if (firstReturnLineIndex > 0)
                {
                    break;
                }
            }

            if (firstReturnLineIndex == -1)
            {
                return new ArgumentException($"ReturnStatementNotFound. {targetComponentName}");
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

        return new SourceLinePoints(leftPaddingCount, firstReturnLineIndex, firstReturnCloseLineIndex);
    }

    internal static
        Task<Result<(IReadOnlyList<string> elementTreeSourceLines, IReadOnlyList<string> importLines)>>
        CalculateElementTreeSourceCodes(ProjectConfig project, IReadOnlyDictionary<string, string> componentConfig, VisualElementModel rootVisualElement)
    {
        return
            // Convert model to node
            from rootNode in ModelToNodeTransformer.ConvertVisualElementModelToReactNodeModel(project, rootVisualElement)

            // Analyze node
            let analyzedRootNode = Plugin.AnalyzeNode(rootNode, componentConfig)

            // Convert node to JSX tree
            from elementJsxTree in ConvertReactNodeModelToElementTreeSourceLines(project, analyzedRootNode, null, 2)

            // Calculate imports
            let importLines = Plugin.CalculateImportLines(analyzedRootNode)

            // return
            select (elementJsxTree, importLines.AsReadOnlyList());
    }

    static async Task<Result<FileModel>> CalculateExportInfo(ExportInput input)
    {
        var (projectId, componentId, userName) = input;

        var user = await Store.TryGetUser(projectId, userName);

        var project = GetProjectConfig(projectId);

        return await
        (
            from data in GetComponentData(new() { ComponentId = componentId, UserName = userName })
            from rootVisualElement in GetComponentUserOrMainVersionAsync(componentId, userName)
            from file in GetComponentFileLocation(componentId, user.LocalWorkspacePath)
            from fileContentInDirectory in FileSystem.ReadAllLines(file.filePath)
            from source in CalculateElementTreeSourceCodes(project, data.Component.GetConfig(), rootVisualElement)
            from formattedSourceLines in Prettier.FormatCode(string.Join(Environment.NewLine, source.elementTreeSourceLines))
            let content = mergeImportLines(fileContentInDirectory, source.importLines)
            from fileContent in InjectRender(content, file.targetComponentName, formattedSourceLines.Split(Environment.NewLine.ToCharArray()))
            select new FileModel
            {
                Path    = file.filePath,
                Content = fileContent
            });

        static IReadOnlyList<string> mergeImportLines(IReadOnlyList<string> fileContentInDirectory, IReadOnlyList<string> importLines)
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

            return fileContentInDirectoryAsList;
        }
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
                var propsAsTextList = new List<string>
                    (
                     from prop in node.Properties
                     where !Design.IsDesignTimeName(prop.Name)
                     select prop switch
                     {
                         _ when prop.Value == "true" => prop.Name,

                         _ when prop.Name == Design.SpreadOperator
                             => '{' + prop.Value + '}',

                         _ when prop.Name == nameof(HtmlElement.dangerouslySetInnerHTML)
                             => $"{prop.Name}={{{{ __html: {prop.Value} }}}}",

                         _ when IsStringValue(prop.Value) =>
                             $"{prop.Name}=\"{TryClearStringValue(prop.Value)}\"",

                         _ when IsStringTemplate(prop.Value) =>
                             $"{prop.Name}={{{prop.Value}}}",

                         _ when elementType.Value
                                    ?.GetProperty(prop.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                                    ?.PropertyType == typeof(string)
                                &&
                                prop.Value.HasMatch(x => x.Contains('/') || x.StartsWith('#') || x.Split(' ').Length > 1)
                             => $"{prop.Name}=\"{prop.Value}\"",

                         _ => $"{prop.Name}={{{prop.Value}}}"
                     }
                    );

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
        return
            from points in GetComponentLineIndexPointsInTsxFile(fileContent, targetComponentName)
            let clearedFileContent = fileContent.ToImmutableList().RemoveRange(points.FirstReturnLineIndex, points.FirstReturnCloseLineIndex - points.FirstReturnLineIndex + 1)
            let linesToInjectPaddingAppliedVersion = applyPadding(linesToInject, points.LeftPaddingCount)
            select string.Join(Environment.NewLine, clearedFileContent.InsertRange(points.FirstReturnLineIndex, linesToInjectPaddingAppliedVersion));

        static IReadOnlyList<string> applyPadding(IReadOnlyList<string> lines, int leftPaddingCount)
        {
            var temp = lines.Select(line => new string(' ', leftPaddingCount + 2) + line).ToList();

            temp.Insert(0, new string(' ', leftPaddingCount) + "return (");

            temp.Add(new string(' ', leftPaddingCount) + ");");

            return temp;
        }
    }

    class LineCollection : List<string>
    {
    }
}