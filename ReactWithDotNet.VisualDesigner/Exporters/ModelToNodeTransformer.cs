using Newtonsoft.Json;
using System.Collections.Immutable;

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

        var hasNoChildAndHasNoText = elementModel.Children.Count == 0 && elementModel.HasNoText();

        if (hasNoChildAndHasNoText)
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
            if (elementModel.HasText())
            {
                children.Add(new()
                {
                    Text = elementModel.GetText(),

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
            var props = new List<ReactProperty>();

            // Transfer properties
            foreach (var property in properties)
            {
                var parsedProperty = ParseProperty(property);
                if (parsedProperty.HasError)
                {
                    return parsedProperty.Error;
                }

                props.Add(new()
                {
                    Name  = parsedProperty.Value.Name,
                    Value = parsedProperty.Value.Value
                });
            }

            if (project.ExportStylesAsInline)
            {
                IReadOnlyList<string> listOfStyleAttributes;
                {
                    var result = ListFrom
                    (
                        from text in styles
                        where !Design.IsDesignTimeName(ParseStyleAttribute(text).Name)
                        from item in CreateDesignerStyleItemFromText(project, text)
                        from finalCssItem in item.FinalCssItems
                        from finalCssItem1 in ReprocessFontWeight(finalCssItem)
                        from value in RecalculateCssValueForOutput(finalCssItem1.Name, finalCssItem1.Value)
                        select $"{KebabToCamelCase(finalCssItem.Name)}: {value}"
                    );
                
                    if (result.HasError)
                    {
                        return result.Error;
                    }
                    
                    listOfStyleAttributes = result.Value;
                }

                if (listOfStyleAttributes.Count > 0)
                {
                    props.Add(new()
                    {
                        Name  = "style",
                        Value = "{" + string.Join(", ", listOfStyleAttributes) + "}"
                    });
                }
            }
            
            if (project.ExportAsCSharp)
            {
                IReadOnlyList<StyleAttribute> listOfStyleAttributes;
                {
                    var result = ListFrom
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
                    );
                
                    if (result.HasError)
                    {
                        return result.Error;
                    }
                    listOfStyleAttributes = result.Value;
                }
                
                
                if (listOfStyleAttributes.Count > 0)
                {
                    props.Add(new()
                    {
                        Name  = "style",
                        Value = Json.Serialize(listOfStyleAttributes)
                    });
                }
            }

            if (project.ExportAsCSharpString)
            {
                IReadOnlyList<FinalCssItem> listOFinalCssItems;
                {
                    var result = ListFrom
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
                    );
                
                    if (result.HasError)
                    {
                        return result.Error;
                    }
                    listOFinalCssItems = result.Value;


                    static Result<DesignerStyleItem> EnsurePseudoIsEmpty(DesignerStyleItem item)
                    {
                        if (item.Pseudo.HasValue())
                        {
                            return new NotSupportedException($"Pseudo styles are not supported in inline styles. {item.OriginalText}");
                        }

                        return Result.From(item);
                    }
                    
                }

                if (listOFinalCssItems.Count > 0)
                {
                    props.Add(new()
                    {
                        Name  = "style",
                        Value = JsonConvert.SerializeObject(listOFinalCssItems, new JsonSerializerSettings{ TypeNameHandling = TypeNameHandling.Auto})
                    });
                }
            }

            return props;

            static Result<FinalCssItem> ReprocessFontWeight(FinalCssItem finalCssItem)
            {
                if (finalCssItem.Name !="font-weight")
                {
                    return Result.From(finalCssItem);
                }
                
                var fontWeightMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "100","thin" },
                    { "200","extralight" },
                    { "300","light" },
                    { "400","normal" },
                    { "500","medium" },
                    { "600","semibold" },
                    { "700","bold" },
                    { "800","extrabold" },
                    { "900","black" }
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
                                
                if (value.StartsWith("request.") || value.StartsWith("context.")) // todo: think better
                {
                    return value;
                } 
                                
                return  '"' + TryClearStringValue(value) + '"';
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

    public ImmutableList<ReactProperty> Properties { get; init; } = [];

    public string Tag { get; init; }

    public string Text { get; init; }

    public required Maybe<Type> HtmlElementType { get; init; }
}

public record ReactProperty
{
    public required string Name { get; init; }
    
    public required string Value { get; init; }
}