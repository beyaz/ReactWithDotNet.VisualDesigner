using Newtonsoft.Json;
using ReactWithDotNet.Transformers;
using System.Collections.Immutable;
using System.Reflection;

namespace ReactWithDotNet.VisualDesigner.Exporters;

static class CSharpExporter
{

    sealed record PropertyTsCode
    {
        public bool IsDesignerSpecificProperty { get; init; }
        public bool IsModifier { get; init; }
        public bool IsStandardAssignment { get; init; }
        public string Text { get; init; }
    }
    public static async Task<Result<string>> CalculateElementTsxCode(int projectId, ComponentConfig componentConfig, VisualElementModel visualElement)
    {        
        var project = GetProjectConfig(projectId);

        return
            from x in await CalculateElementTreeSourceCodes(project, componentConfig, visualElement)
            select string.Join(Environment.NewLine, x.elementTreeSourceLines);        
    }

    public static Task<Result<(bool HasChange,FileModel File)>> ExportToFileSystem(ExportInput input)
    {
        return
            from file in CalculateExportInfo(input)
            from fileContentAtDisk in FileSystem.ReadAllText(file.Path)
            select IsEqualsIgnoreWhitespace(fileContentAtDisk, file.Content) switch
            {
                true => Result.From((false,file)),
                false =>
                    from _ in FileSystem.Save(file)
                    select (true,file)
            };
    }

    public static Result<SourceLinePoints> GetComponentLineIndexPointsInCSharpFile(IReadOnlyList<string> fileContent, string targetComponentName)
    {
        var lines = fileContent.ToList();

        // maybe  ZoomComponent:View
        {
            var names = targetComponentName.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length == 2)
            {
                var className = names[0];
                var methodName = names[1];

                var classDeclarationLineIndex = lines.FindIndex(line => line.Contains($"class {className} "));
                if (classDeclarationLineIndex >= 0)
                {
                    var methodDeclerationLineIndex = lines.FindIndex(classDeclarationLineIndex, line => line.Contains($" Element {methodName}("));
                    if (methodDeclerationLineIndex >= 0)
                    {
                        var leftSpaceCount = Array.FindIndex(lines[methodDeclerationLineIndex].ToCharArray(), c => c != ' ');
                        var firstReturnLineIndex = -1;
                        var leftPaddingCount = -1;
                        {
                            foreach (var item in lines.FindLineIndexStartsWith(methodDeclerationLineIndex, leftSpaceCount + 4, "return "))
                            {
                                firstReturnLineIndex = item;
                                leftPaddingCount     = leftSpaceCount + 4;
                            }

                            if (firstReturnLineIndex < 0)
                            {
                                return new InvalidOperationException("No return found");
                            }
                        }

                        if (lines[firstReturnLineIndex].EndsWith(";"))
                        {
                            return new SourceLinePoints(leftPaddingCount, firstReturnLineIndex, firstReturnLineIndex);
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

                        return new SourceLinePoints(leftPaddingCount, firstReturnLineIndex, firstReturnCloseLineIndex);
                    }
                }
            }
        }

        return new ArgumentException($"ComponentDeclarationNotFoundInFile. {targetComponentName}");
    }

    internal static Task<Result<(IReadOnlyList<string> elementTreeSourceLines, IReadOnlyList<string> importLines)>> CalculateElementTreeSourceCodes(ProjectConfig project, ComponentConfig componentConfig, VisualElementModel rootVisualElement)
    {
        return
            // Convert model to node
            from rootNode in ModelToNodeTransformer.ConvertVisualElementModelToReactNodeModel(project, rootVisualElement)

            // Analyze node
            let analyzedRootNode = AnalyzeNode(rootNode, componentConfig)

            // Convert node to JSX tree
            from elementJsxTree in ConvertReactNodeModelToElementTreeSourceLines(project, analyzedRootNode, null, 0)

            // Calculate imports
            let importLines = CalculateImportLines(analyzedRootNode)

            // return
            select (elementJsxTree, importLines.AsReadOnlyList());
    }

    static async Task<Result<FileModel>> CalculateExportInfo(ExportInput input)
    {
        var (projectId, componentId, userName) = input;

        var user = await Store.TryGetUser(projectId, userName);

        var project = GetProjectConfig(projectId);

        return await
            (from data in GetComponentData(componentId, userName)
            from rootVisualElement in GetComponentUserOrMainVersionAsync(componentId, userName)
            from file in GetComponentFileLocation(componentId, userName)
            from fileContentInDirectory in FileSystem.ReadAllLines(file.filePath)
            from source in CalculateElementTreeSourceCodes(project, data.Component.Config, rootVisualElement)
            from fileContent in InjectRender(fileContentInDirectory, file.targetComponentName, source.elementTreeSourceLines)
            select new FileModel
            {
                Path    = file.filePath,
                Content = fileContent
            });
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

                lines.Add($"{indent(indentLevel)}from item in {itemsSource.Value}");

                lines.Add(indent(indentLevel) + "select ");

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
                            line = line.TrimStart().RemoveFromStart("(");

                            lines[^1] += line;
                            continue;
                        }

                        // is last line
                        if (i == innerLines.Count - 1)
                        {
                            line = line.TrimEnd().RemoveFromEnd(")");
                        }

                        lines.Add(line);
                    }
                }

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

                var tag = component.Config.Name;
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
                                    var (success, modifierCode) = ToModifierTransformer.TryConvertToModifier(isStyleValue: false, elementType.Value.Name, propertyName, propertyValue);
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
                                childElementSourceLines = childElementSourceLines.SetItem(childElementSourceLines.Count - 1, childElementSourceLines[^1] + ",");
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

            List<PropertyTsCode> tsPropTexts;
            string partProps;
            {
                var modifiers = new List<string>();
                {
                    
                    {
                        tsPropTexts = new
                            (
                             from reactProperty in from p in node.Properties where p.Name.NotIn(Design.Text, Design.TextPreview, Design.Src, Design.Name, "style") select p
                             let propertyTsCode = convertReactPropertyToString(elementType, reactProperty)
                             select propertyTsCode
                            );
                        
                         static PropertyTsCode convertReactPropertyToString(Maybe<Type> elementType, ReactProperty reactProperty)
                        {
                            var propertyName = reactProperty.Name;

                            var propertyValue = reactProperty.Value;

                            if (propertyName is Design.ItemsSource || propertyName is Design.ItemsSourceDesignTimeCount)
                            {
                                return new ()
                                {
                                    IsDesignerSpecificProperty = true
                                };
                            }

                            if (elementType.HasValue)
                            {
                                var (success, modifierCode) = ToModifierTransformer.TryConvertToModifier(isStyleValue: false, elementType.Value.Name, propertyName, propertyValue);
                                if (success)
                                {
                                    if (!IsStringValue(propertyValue))
                                    {
                                        return new ()
                                        {
                                            IsModifier = true,
                                            Text       = modifierCode.Replace('"' + propertyValue + '"', propertyValue)
                                        };
                                    }
                                    
                                    return new ()
                                    {
                                        IsModifier = true,
                                        Text = modifierCode
                                    };
                                }
                            }

                            

                            if (IsStringValue(propertyValue))
                            {
                                return new ()
                                {
                                    IsStandardAssignment   = true,
                                    
                                    Text = $"{propertyName} = \"{TryClearStringValue(propertyValue)}\""
                                };
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
                                            return new ()
                                            {
                                                IsStandardAssignment = true,
                                                
                                                Text = $"{propertyName} = \"{propertyValue}\""
                                            };
                                        }
                                    }
                                }
                            }

                            return new ()
                            {
                                IsStandardAssignment = true,
                                                
                                Text = $"{propertyName} = {propertyValue}"
                            };
                        }
                    }
                    
                    // import props except style
                    {
                        tsPropTexts.RemoveAll(x => x.IsDesignerSpecificProperty);

                        modifiers.AddRange(from x in tsPropTexts where x.IsModifier select x.Text);
                        
                        tsPropTexts.RemoveAll(x => x.IsModifier);
                    }

                    // import style
                    {
                        List<string> styleList = [];
                        {
                            foreach (var styleAttribute in
                                     from reactProperty in from p in node.Properties where p.Name == "style" select p
                                     from styleAttribute in JsonConvert.DeserializeObject<IReadOnlyList<StyleAttribute>>(reactProperty.Value)
                                     where !Design.IsDesignTimeName(styleAttribute.Name)
                                     select styleAttribute)
                            {
                                var tagName = elementType.Value?.Name;

                                var attributeValue = TryClearStringValue(styleAttribute.Value);

                                // try parse condition
                                {
                                    var (success, condition, left, right) = TryParseConditionalValue(TryClearStringValue(styleAttribute.Value));
                                    if (success)
                                    {
                                        if (left.HasValue() && right.HasValue())
                                        {
                                            var modifierCodeForLeft = ToModifierTransformer.TryConvertToModifier(isStyleValue: true, tagName, styleAttribute.Name, left);

                                            var modifierCodeForRight = ToModifierTransformer.TryConvertToModifier(isStyleValue: true, tagName, styleAttribute.Name, right);

                                            if (modifierCodeForLeft.success && modifierCodeForRight.success)
                                            {
                                                styleList.Add($"{condition} ? {modifierCodeForLeft.modifierCode} : {modifierCodeForRight.modifierCode}");
                                                continue;
                                            }
                                        }
                                    }
                                }

                                // try import from modifier
                                {
                                    var modifierCode = ToModifierTransformer.TryConvertToModifier(isStyleValue: true, tagName, styleAttribute.Name, attributeValue);
                                    if (modifierCode.success)
                                    {
                                        if (styleAttribute.Pseudo.HasNoValue())
                                        {
                                            styleList.Add(modifierCode.modifierCode);
                                            continue;
                                        }

                                        var pseudo = ToModifierTransformer.TryGetPseudoForCSharp(styleAttribute.Pseudo);
                                        if (pseudo.success)
                                        {
                                            styleList.Add($"{pseudo.pseudo}({modifierCode.modifierCode})");
                                            continue;
                                        }

                                        return new ArgumentException("NotResolved:" + styleAttribute.Pseudo);
                                    }
                                }

                                styleList.Add($"CreateStyleModifier(x=>x.{styleAttribute.Name} = ({styleAttribute.Value})");
                            }
                        }

                        modifiers.AddRange(styleList);
                    }
                }

                if (modifiers.Count > 0)
                {
                    partProps = "(" + string.Join(", ", modifiers) + ")";
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
                if (tsPropTexts.Count > 0)
                {
                    return new LineCollection
                    {
                        $"{indent(indentLevel)}new {tag}{partProps.RemoveFromEnd("()")}",
                        indent(indentLevel) + "{",
                        
                        from x in tsPropTexts.Select((x,i)=>new
                        {
                            text = indent(indentLevel+1) +  x.Text,  
                            
                            isLast =i==tsPropTexts.Count-1
                        })
                        
                        select x.isLast switch
                        {
                            false =>x.text + ",",
                            true =>x.text
                        },
                        
                        
                        indent(indentLevel) + "}",
                        
                    };
                }
                return new LineCollection
                {
                    $"{indent(indentLevel)}new {tag}{partProps}"
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
                $"{indent(indentLevel)}new {tag}{partProps}",
                indent(indentLevel) + "{"
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
                        childElementSourceLines = childElementSourceLines.SetItem(childElementSourceLines.Count - 1, childElementSourceLines[^1] + ",");
                    }
                }

                lines.AddRange(childElementSourceLines);

                childIndex++;
            }

            // Close tag
            lines.Add(indent(indentLevel) + "}");

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
                return text;
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

            return '"' + TryClearStringValue(text) + '"';
        }

        static string indent(int indentLevel)
        {
            const int IndentLength = 4;

            return new(' ', indentLevel * IndentLength);
        }
    }

    static Result<string> InjectRender(IReadOnlyList<string> fileContent, string targetComponentName, IReadOnlyList<string> linesToInject)
    {
        return
            from points in GetComponentLineIndexPointsInCSharpFile(fileContent, targetComponentName)
            let clearedFileContent = fileContent.ToImmutableList().RemoveRange(points.FirstReturnLineIndex, points.FirstReturnCloseLineIndex - points.FirstReturnLineIndex + 1)
            let linesToInjectPaddingAppliedVersion = applyPadding(linesToInject, points.LeftPaddingCount)
            select string.Join(Environment.NewLine, clearedFileContent.InsertRange(points.FirstReturnLineIndex, linesToInjectPaddingAppliedVersion));

        static IReadOnlyList<string> applyPadding(IReadOnlyList<string> lines, int leftPaddingCount)
        {
            var temp = lines.Select(line => new string(' ', leftPaddingCount) + line).ToList();

            temp[0] = new string(' ', leftPaddingCount) + "return " + temp[0].Trim();

            temp[^1] += ";";

            return temp;
        }
    }

    class LineCollection : List<string>
    {
        public void Add(IEnumerable<string> values)
        {
            AddRange(values);
        }
    }
}