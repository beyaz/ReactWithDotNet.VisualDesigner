using System.Collections.Immutable;
using Newtonsoft.Json;

namespace ReactWithDotNet.VisualDesigner.Exporters;

static class ModelToNodeTransformer
{
    public static async Task<Result<ReactNode>> ConvertVisualElementModelToReactNodeModel(ProjectConfig project, VisualElementModel elementModel)
    {
        var htmlElementType = TryGetHtmlElementTypeByTagName(elementModel.Tag);

        ImmutableList<ReactProperty> properties;
        {
            var props = project switch
            {
                _ when project.ExportStylesAsInline || project.ExportAsCSharp || project.ExportAsCSharpString
                    => calculatePropsForInlineStyle(project, elementModel.Properties, elementModel.Styles),

                _ when project.ExportStylesAsTailwind
                    => calculatePropsForTailwind(project, elementModel.Properties, elementModel.Styles),

                _ => new ArgumentException("Style export not specified")
            };

            if (props.HasError)
            {
                return props.Error;
            }

            properties = props.Value.ToImmutableList();
        }

        var hasNoChildAndHasNoContent = elementModel.Children.Count == 0 && elementModel.HasNoContent;

        if (hasNoChildAndHasNoContent)
        {
            return new ReactNode
            {
                Tag = elementModel.Tag,

                HtmlElementType = htmlElementType,

                Properties = properties
            };
        }

        List<ReactNode> children = [];
        {
            // Add text content
            if (elementModel.HasContent)
            {
                children.Add(new()
                {
                    Text = elementModel.Content,

                    HtmlElementType = None
                });
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

                children.Add(childNode);
            }
        }

        return new ReactNode
        {
            Tag = elementModel.Tag,

            HtmlElementType = htmlElementType,

            Properties = properties,

            Children = children.ToImmutableList()
        };

        static Result<IReadOnlyList<ReactProperty>> calculatePropsForInlineStyle(ProjectConfig project, IReadOnlyList<string> properties, IReadOnlyList<string> styles)
        {
            var styleProp = project switch
            {
                { ExportStylesAsInline: true } =>

                    from listOfStyleAttributes in ListFrom
                    (
                        from text in styles
                        where !Design.IsDesignTimeName(ParseStyleAttribute(text).Name)
                        from item in CreateDesignerStyleItemFromText(project, text)
                        from finalCssItem in item.FinalCssItems
                        from finalCssItem1 in ReprocessFontWeight(finalCssItem)
                        from value in RecalculateCssValueForOutput(finalCssItem1.Name, finalCssItem1.Value)
                        select $"{KebabToCamelCase(finalCssItem.Name)}: {value}"
                    )
                    select (listOfStyleAttributes.Count > 0) switch
                    {
                        true => new ReactProperty
                        {
                            Name  = "style",
                            Value = "{" + string.Join(", ", listOfStyleAttributes) + "}"
                        },
                        false => null
                    },

                { ExportAsCSharp: true } =>

                    from listOfStyleAttributes in ListFrom
                    (
                        from text in styles
                        where !Design.IsDesignTimeName(ParseStyleAttribute(text).Name)
                        from item in CreateDesignerStyleItemFromText(project, text)
                        from finalCssItem in item.FinalCssItems
                        select new StyleAttribute
                        {
                            Pseudo = item.Pseudo,

                            Name = KebabToCamelCase(finalCssItem.Name),

                            Value = finalCssItem.Value switch
                            {
                                null => null,

                                var x when x.StartsWith("request.") || x.StartsWith("context.") => x,

                                var x => '"' + TryClearStringValue(x) + '"'
                            }
                        }
                    )
                    select (listOfStyleAttributes.Count > 0) switch
                    {

                        true => new ReactProperty
                        {
                            Name  = "style",
                            Value = Json.Serialize(listOfStyleAttributes)
                        },
                        false => null
                    },

                { ExportAsCSharpString: true } =>

                    from listOFinalCssItems in ListFrom
                    (
                        from text in styles
                        where !Design.IsDesignTimeName(ParseStyleAttribute(text).Name)
                        from item in CreateDesignerStyleItemFromText(project, text)
                        from _ in EnsurePseudoIsEmpty(item)
                        from finalCssItem in item.FinalCssItems
                        from finalValue in CreateFinalCssItem(new()
                        {
                            Name = finalCssItem.Name,

                            Value = finalCssItem.Value switch
                            {
                                null => null,

                                var x when x.StartsWith("request.") || x.StartsWith("context.") => x,

                                var x => TryClearStringValue(x)
                            }
                        })
                        select finalValue
                    )
                    select (listOFinalCssItems.Count > 0) switch
                    {
                        true => new ReactProperty
                        {
                            Name  = "style",
                            Value = JsonConvert.SerializeObject(listOFinalCssItems, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto })
                        },
                        false => null
                    },
                _ => Result.Error<ReactProperty>(new($"Project config Error. Specify {nameof(ProjectConfig.ExportAsCSharp)} or {nameof(ProjectConfig.ExportAsCSharpString)} or {nameof(ProjectConfig.ExportStylesAsInline)}"))
            };
            
            
            return ListFrom
            (
                from property in properties
                from parsedProperty in ParseProperty(property)
                select new ReactProperty
                {
                    Name  = parsedProperty.Name,
                    Value = parsedProperty.Value
                },
                
                from reactPropertyResult in ListFrom(styleProp) 
                from reactProperty in reactPropertyResult
                where reactProperty is not null 
                select reactProperty

            );

            static Result<DesignerStyleItem> EnsurePseudoIsEmpty(DesignerStyleItem item)
            {
                if (item.Pseudo.HasValue)
                {
                    return new NotSupportedException($"Pseudo styles are not supported in inline styles. {item.OriginalText}");
                }

                return Result.From(item);
            }

            static Result<FinalCssItem> ReprocessFontWeight(FinalCssItem finalCssItem)
            {
                if (finalCssItem.Name != "font-weight")
                {
                    return Result.From(finalCssItem);
                }

                var fontWeightMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "100", "thin" },
                    { "200", "extralight" },
                    { "300", "light" },
                    { "400", "normal" },
                    { "500", "medium" },
                    { "600", "semibold" },
                    { "700", "bold" },
                    { "800", "extrabold" },
                    { "900", "black" }
                };

                if (fontWeightMap.TryGetValue(finalCssItem.Value, out var weightAsName))
                {
                    return CreateFinalCssItem(finalCssItem.Name, weightAsName);
                }

                return Result.From(finalCssItem);
            }

            static Result<string> RecalculateCssValueForOutput(string name, string value)
            {
                var parseResult = TryParseConditionalValue(value);
                if (parseResult.success)
                {
                    if (parseResult.left is null)
                    {
                        return new ArgumentException($"{name} left condition has no value.");
                    }

                    if (parseResult.right is null)
                    {
                        return new ArgumentException($"{name} right condition has no value.");
                    }

                    return
                        from left in RecalculateCssValueForOutput(name, parseResult.left)
                        from right in RecalculateCssValueForOutput(name, parseResult.right)
                        select $"{parseResult.condition} ? {left} : {right}";
                }

                if (value.StartsWith("request.") || 
                    value.StartsWith("context.") || 
                    value.Contains("(index)")) // todo: think better
                {
                    return value;
                }

                return '"' + TryClearStringValue(value) + '"';
            }
        }

        static Result<IReadOnlyList<ReactProperty>> calculatePropsForTailwind(ProjectConfig project, IReadOnlyList<string> properties, IReadOnlyList<string> styles)
        {
            var props = new List<ReactProperty>();

            List<string> classNames = [];

            var classNameShouldBeTemplateLiteral = false;

            // Transfer properties
            foreach (var property in properties)
            {
                var parsedProperty = ParseProperty(property);
                if (parsedProperty.HasError)
                {
                    return parsedProperty.Error;
                }

                if (parsedProperty.Value.Name == "class")
                {
                    classNames.AddRange(parsedProperty.Value.Value.Split(" ", StringSplitOptions.RemoveEmptyEntries));
                    continue;
                }

                props.Add(new()
                {
                    Name  = parsedProperty.Value.Name,
                    Value = parsedProperty.Value.Value
                });
            }

            foreach (var styleItem in styles)
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

                props.Add(new()
                {
                    Name  = "className",
                    Value = firstLastChar + string.Join(" ", classNames) + firstLastChar
                });
            }

            return props;
        }
    }
}

public record ReactNode
{
    public ImmutableList<ReactNode> Children { get; init; } = [];

    public required Maybe<Type> HtmlElementType { get; init; }

    public ImmutableList<ReactProperty> Properties { get; init; } = [];

    public string Tag { get; init; }

    public string Text { get; init; }
}

public record ReactProperty
{
    public required string Name { get; init; }

    public required string Value { get; init; }
}