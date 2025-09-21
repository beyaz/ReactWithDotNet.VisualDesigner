using System.IO;
using ReactWithDotNet.Transformers;

namespace ReactWithDotNet.VisualDesigner.Exporters;

static class CSharpStringExporter
{
    public static async Task<Result<string>> CalculateElementTsxCode(int projectId, IReadOnlyDictionary<string, string> componentConfig, VisualElementModel visualElement)
    {
        var project = GetProjectConfig(projectId);

        var result = await CalculateElementTreeSourceCodes(project, componentConfig, visualElement);
        if (result.HasError)
        {
            return result.Error;
        }

        return string.Join(Environment.NewLine, result.Value.elementTreeSourceLines);
    }

    public static async Task<Result<ExportOutput>> ExportToFileSystem(ExportInput input)
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

        if (IsEqualsIgnoreWhitespace(fileContentAtDisk, fileContent))
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
    }

    public static Result<(int leftPaddingCount, int firstReturnLineIndex, int firstReturnCloseLineIndex)>
        GetComponentLineIndexPointsInCSharpFile(IReadOnlyList<string> fileContent, string targetComponentName)
    {
        var lines = fileContent.ToList();

        // maybe  ZoomComponent:View
        {
            var names = targetComponentName.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length != 2)
            {
                return new ArgumentException($"ComponentDeclerationNotFoundInFile. {targetComponentName}");
            }

            var className = names[0];
            var methodName = names[1];

            var methodDeclarationLineIndex = -1;
            {
                foreach (var lineIndex in RoslynHelper.FindMethodStartLineIndexInCSharpCode(lines, className, methodName))
                {
                    methodDeclarationLineIndex = lineIndex;
                }

                if (methodDeclarationLineIndex < 0)
                {
                    var classDeclerationLineIndex = lines.FindIndex(line => line.Contains($"class {className} "));
                    if (classDeclerationLineIndex < 0)
                    {
                        return new ArgumentException($"ComponentDeclerationNotFoundInFile. {targetComponentName}");
                    }

                    methodDeclarationLineIndex = lines.FindIndex(classDeclerationLineIndex, line => line.Contains($" Element {methodName}("));
                    if (methodDeclarationLineIndex < 0)
                    {
                        return new ArgumentException($"ComponentDeclerationNotFoundInFile. {targetComponentName}");
                    }
                }
            }

            var leftSpaceCount = Array.FindIndex(lines[methodDeclarationLineIndex].ToCharArray(), c => c != ' ');

            var firstReturnLineIndex = -1;
            var leftPaddingCount = -1;
            {
                foreach (var item in lines.FindLineIndexStartsWith(methodDeclarationLineIndex, leftSpaceCount + 4, "return ", "return"))
                {
                    firstReturnLineIndex = item;
                    leftPaddingCount     = item + 4;
                }

                if (firstReturnLineIndex < 0)
                {
                    return new InvalidOperationException("No return found");
                }
            }

            if (lines[firstReturnLineIndex].EndsWith(";"))
            {
                return (leftPaddingCount, firstReturnLineIndex, firstReturnLineIndex);
            }

            var firstReturnCloseLineIndex = -1;
            {
                foreach (var item in lines.FindLineIndexStartsWith(firstReturnLineIndex, leftSpaceCount + 4, "};"))
                {
                    firstReturnCloseLineIndex = item;
                }

                if (firstReturnCloseLineIndex < 0)
                {
                    return new InvalidOperationException("No return found");
                }
            }

            return (leftPaddingCount, firstReturnLineIndex, firstReturnCloseLineIndex);
        }
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
            var result = await ConvertReactNodeModelToElementTreeSourceLines(project, rootNode, null, 0);
            if (result.HasError)
            {
                return result.Error;
            }

            elementJsxTree = result.Value;

            elementJsxTree = appendDollarSignAtLineStartIfNeed(appendCommaEndOfLine(elementJsxTree));
        }

        var importLines = Plugin.CalculateImportLines(rootNode);

        return (elementJsxTree, importLines.ToList());

        static List<string> appendCommaEndOfLine(IReadOnlyList<string> lines)
        {
            return ListFrom(from line in lines.Select((line, index) => new { text = line, index })
                            let length = lines.Count - 1
                            let isLastLine = line.index == lines.Count - 1
                            let lineStartsWithDoubleQuote = line.text.TrimStart().StartsWith('"')
                            let lineEndsWithDoubleQuote = line.text.TrimEnd().EndsWith('"')
                            let hasNextLine = !isLastLine
                            let nextLineIsEndOfMapping = hasNextLine && lines[line.index + 1].TrimStart().StartsWith("},")
                            select line switch
                            {
                                _ when !isLastLine &&
                                       lineStartsWithDoubleQuote &&
                                       lineEndsWithDoubleQuote &&
                                       !nextLineIsEndOfMapping => line.text + ",",

                                _ => line.text
                            });
        }

        static List<string> appendDollarSignAtLineStartIfNeed(IReadOnlyList<string> lines)
        {
            return ListFrom(from line in lines.Select((line, index) => new { text = line, index })
                            let length = lines.Count
                            let lineStartsWithDoubleQuote = line.text.TrimStart().StartsWith('"')
                            let lineContainsLeftBracket = line.text.Contains('{')
                            select line switch
                            {
                                _ when lineStartsWithDoubleQuote && lineContainsLeftBracket => '$' + line.text.TrimStart(),

                                _ => line.text
                            });
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
            IReadOnlyList<string> fileContentInDirectory;
            {
                var result = await IO.TryReadFileAllLines(filePath);
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
                $"{indent(indentLevel)}{asFinalText(node.Children[0].Text)}"
            };
        }

        if (nodeTag is null)
        {
            if (node.Text.HasValue())
            {
                return new List<string>
                {
                    $"{indent(indentLevel)}{asFinalText(node.Text)}"
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

                lines.Add($"{indent(indentLevel)}from item in {itemsSource.Value}");

                IReadOnlyList<string> innerLines;
                {
                    var result = await ConvertReactNodeModelToElementTreeSourceLines(project, node, parentNode, indentLevel);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    innerLines = result.Value;
                }

                lines.Add(indent(indentLevel) + "select new LineCollection");
                lines.Add(indent(indentLevel) + "{");

                lines.AddRange(innerLines);

                lines.Add(indent(indentLevel) + "},");

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
                    $"{indent(indentLevel)}!{showIf.Value} ? null :"
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
                lines.Add($"{indent(indentLevel)}");

                return lines;
            }

            if (hideIf is not null)
            {
                node = node with { Properties = node.Properties.Remove(hideIf) };

                List<string> lines =
                [
                    $"{indent(indentLevel)}{hideIf.Value} ? null :"
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
                lines.Add($"{indent(indentLevel)}");

                return lines;
            }
        }

        // is component
        {
            if (int.TryParse(nodeTag, out var componentId))
            {
                var component = await Store.TryGetComponent(componentId);
                if (component is null)
                {
                    return new ArgumentNullException($"ComponentNotFound. {componentId}");
                }

                var tag = component.GetName();
                if (tag.EndsWith("::render"))
                {
                    tag = tag.Replace("::render", string.Empty);
                }

                var elementType = node.HtmlElementType;

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

                var hasNoBody = node.Children.Count == 0 && node.Text.HasNoValue() && childrenProperty is null && node.Properties.Count == 0;

                List<string> propsAsTextList;
                string partProps;
                {
                    propsAsTextList = [];
                    {
                        // import props
                        {
                            var propsWithoutStyle =
                                from reactProperty in node.Properties
                                let text = convertReactPropertyToString(elementType, reactProperty)
                                where text is not null
                                select IsStringValue(reactProperty.Value) switch
                                {
                                    true  => text,
                                    false => text.Replace('"' + reactProperty.Value + '"', reactProperty.Value)
                                };

                            propsAsTextList.AddRange(propsWithoutStyle);

                            static string convertReactPropertyToString(Maybe<Type> elementType, ReactProperty reactProperty)
                            {
                                var propertyName = reactProperty.Name;

                                var propertyValue = reactProperty.Value;

                                if (propertyName is Design.ItemsSource || propertyName is Design.ItemsSourceDesignTimeCount)
                                {
                                    return null;
                                }

                                if (elementType.HasValue)
                                {
                                    var (success, modifierCode) = ToModifierTransformer.TryConvertToModifier(elementType.Value.Name, propertyName, TryClearStringValue(propertyValue));
                                    if (success)
                                    {
                                        return modifierCode;
                                    }
                                }

                                if (IsStringValue(propertyValue))
                                {
                                    return $"{propertyName}=\"{TryClearStringValue(propertyValue)}\"";
                                }

                                return $"{propertyName}={propertyValue}";
                            }
                        }
                    }

                    if (propsAsTextList.Count > 0)
                    {
                        partProps = string.Empty;
                    }
                    else
                    {
                        if (hasNoBody)
                        {
                            partProps = "()";
                        }
                        else
                        {
                            partProps = string.Empty;
                        }
                    }
                }

                if (hasNoBody)
                {
                    return new LineCollection
                    {
                        $"{indent(indentLevel)}new {tag}()"
                    };
                }

                // children property
                {
                    // sample: children: state.suggestionNodes

                    if (childrenProperty is not null)
                    {
                        if (propsAsTextList.Any())
                        {
                            var lineCollection = new LineCollection
                            {
                                $"{indent(indentLevel)}new {tag}{partProps}",
                                indent(indentLevel) + "{"
                            };

                            indentLevel++;

                            foreach (var line in propsAsTextList)
                            {
                                lineCollection.Add(line + ",");
                            }

                            lineCollection.Add(indent(indentLevel) + "children =");
                            lineCollection.Add(indent(indentLevel) + "{");

                            lineCollection.Add($"{indent(indentLevel + 1)}{childrenProperty.Value}");

                            lineCollection.Add(indent(indentLevel) + "}");
                            indentLevel--;

                            lineCollection.Add(indent(indentLevel) + "}");

                            return lineCollection;
                        }

                        return new LineCollection
                        {
                            $"{indent(indentLevel)}new {tag}",
                            indent(indentLevel) + "{",
                            $"{indent(indentLevel + 1)}{childrenProperty.Value}",
                            indent(indentLevel) + "}"
                        };
                    }
                }

                LineCollection lines =
                [
                    $"{indent(indentLevel)}new {tag}{partProps}",
                    indent(indentLevel) + "{"
                ];

                // add properties
                {
                    indentLevel++;
                    foreach (var line in propsAsTextList)
                    {
                        lines.Add(indent(indentLevel) + line + ",");
                    }
                }

                if (node.Children.Any())
                {
                    lines.Add(indent(indentLevel) + "children =");

                    // open bracket
                    {
                        lines.Add(indent(indentLevel) + "{");
                        indentLevel++;
                    }

                    // Add children
                    var childIndex = 0;

                    foreach (var child in node.Children)
                    {
                        IReadOnlyList<string> childElementSourceLines;
                        {
                            var result = await ConvertReactNodeModelToElementTreeSourceLines(project, child, node, indentLevel + 1);
                            if (result.HasError)
                            {
                                return result.Error;
                            }

                            childElementSourceLines = result.Value;

                            // add comma at end of child element except last
                            if (childIndex < node.Children.Count - 1 && childElementSourceLines.Count > 0)
                            {
                                // childElementSourceLines = childElementSourceLines.SetItem(childElementSourceLines.Count - 1, childElementSourceLines[^1] + ",");
                            }
                        }

                        lines.AddRange(childElementSourceLines);

                        childIndex++;
                    }

                    // close bracket
                    {
                        indentLevel--;
                        lines.Add(indent(indentLevel) + "}");
                    }
                }
                else
                {
                    lines[^1] = lines[^1].RemoveFromEnd(",");
                }

                indentLevel--;
                // Close tag
                lines.Add(indent(indentLevel) + "}");

                return lines;
            }
        }

        {
            var elementType = node.HtmlElementType;

            var tag = nodeTag;

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

            var hasNoBody = node.Children.Count == 0 && node.Text.HasNoValue() && childrenProperty is null;

            string partProps;
            {
                var propsAsTextList = new List<string>();
                {
                    // import props except style
                    {
                        foreach (var reactProperty in from p in node.Properties where p.Name != "style" select p)
                        {
                            if (Design.IsDesignTimeName(reactProperty.Name))
                            {
                                continue;
                            }

                            var value = reactProperty.Value;

                            var finalValue = IsStringValue(value) switch
                            {
                                true  => '\\'.ToString() + '"' + TryClearStringValue(value) + '\\' + '"',
                                false => value
                            };

                            propsAsTextList.Add($"{reactProperty.Name}={finalValue}");
                        }
                    }

                    // import style
                    {
                        List<(string name, string value)> listOfCss = [];
                        {
                            foreach (var json in from p in node.Properties where p.Name == "style" select p.Value)
                            {
                                foreach (var styleAttribute in Json.Deserialize<IReadOnlyList<FinalCssItem>>(json))
                                {
                                    if (Design.IsDesignTimeName(styleAttribute.Name))
                                    {
                                        continue;
                                    }

                                    listOfCss.Add((styleAttribute.Name, styleAttribute.Value));
                                }
                            }
                        }

                        if (listOfCss.Count > 0)
                        {
                            propsAsTextList.Add($"style=\\\"{string.Join("; ", from x in listOfCss select $"{x.name}: {x.value}")};\\\"");
                        }
                    }
                }

                if (propsAsTextList.Count > 0)
                {
                    partProps = " " + string.Join(" ", propsAsTextList) + ">";
                }
                else
                {
                    if (hasNoBody)
                    {
                        partProps = " />";
                    }
                    else
                    {
                        partProps = ">";
                    }
                }
            }

            if (hasNoBody)
            {
                return new LineCollection
                {
                    '"' + $"{indent(indentLevel)}<{tag}{partProps}" + '"'
                };
            }

            // children property
            {
                // sample: children: state.suggestionNodes

                if (childrenProperty is not null)
                {
                    return new LineCollection
                    {
                        $"{indent(indentLevel)}new {tag}{partProps}",
                        indent(indentLevel) + "{",
                        $"{indent(indentLevel + 1)}{childrenProperty.Value}",
                        indent(indentLevel) + "}"
                    };
                }
            }

            LineCollection lines =
            [
                $"\"{indent(indentLevel)}<{tag}{partProps}\""
            ];

            // Add children
            var childIndex = 0;

            foreach (var child in node.Children)
            {
                IReadOnlyList<string> childElementSourceLines;
                {
                    var result = await ConvertReactNodeModelToElementTreeSourceLines(project, child, node, indentLevel + 1);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    childElementSourceLines = result.Value;

                    // add comma at end of child element except last
                    if (childIndex < node.Children.Count - 1 && childElementSourceLines.Count > 0)
                    {
                        // childElementSourceLines = childElementSourceLines.SetItem(childElementSourceLines.Count - 1, childElementSourceLines[^1] + ",");
                    }
                }

                lines.AddRange(childElementSourceLines);

                childIndex++;
            }

            // Close tag
            lines.Add('"' + $"{indent(indentLevel)}</{tag}>" + '"');

            return lines;
        }

        static string asFinalText(string text)
        {
            if (!IsStringValue(text))
            {
                return text + ",";
            }

            return FirstOf
                (
                 from x in new[] { text }
                 let clear = TryClearStringValue(x)
                 let quoteCount = clear.Contains('"') ? 3 : 1
                 let quote = new string('"', quoteCount)
                 select quote + clear + quote
                );
        }

        static string indent(int indentLevel)
        {
            const int IndentLength = 4;

            return new(' ', indentLevel * IndentLength);
        }
    }

    static Result<string> InjectRender(IReadOnlyList<string> fileContent, string targetComponentName, IReadOnlyList<string> linesToInject)
    {
        var lines = fileContent.ToList();

        // focus to component code
        int firstReturnLineIndex, firstReturnCloseLineIndex, leftPaddingCount;
        {
            var result = GetComponentLineIndexPointsInCSharpFile(fileContent, targetComponentName);
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
            var temp = linesToInject.Select(line => new string(' ', leftPaddingCount + 4) + line).ToList();

            temp.Insert(0, new string(' ', leftPaddingCount) + "return new LineCollection");

            temp.Insert(1, new string(' ', leftPaddingCount) + "{");

            temp.Add(new string(' ', leftPaddingCount) + "};");

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