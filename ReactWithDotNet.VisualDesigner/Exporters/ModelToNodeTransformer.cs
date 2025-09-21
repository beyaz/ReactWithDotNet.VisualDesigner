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

                _ => Fail<IReadOnlyList<ReactProperty>>(new ArgumentException("Style export not specified"))
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

            if (project.ExportAsCSharp)
            {
                if (styles.Count == 0)
                {
                    return props;
                }

                string stlyeValueAsJson;
                {
                    List<StyleAttribute> listOfStyleAttributes = [];
                    {
                        foreach (var text in styles)
                        {
                            if (Design.IsDesignTimeName(ParseStyleAttribute(text).Name))
                            {
                                continue;
                            }

                            var item = CreateDesignerStyleItemFromText(project, text);
                            if (item.HasError)
                            {
                                return item.Error;
                            }

                            foreach (var x in item.Value.FinalCssItems)
                            {
                                var styleAttribute = new StyleAttribute
                                {
                                    Pseudo = item.Value.Pseudo,
                                    Name = project.ExportAsCSharpString switch
                                    {
                                        true  => x.Name,
                                        false => KebabToCamelCase(x.Name)
                                    },

                                    Value = x.Value switch
                                    {
                                        null => null,

                                        var y when y.StartsWith("request.") || y.StartsWith("context.") => y,

                                        var y => project.ExportAsCSharpString switch
                                        {
                                            true  => TryClearStringValue(y),
                                            false => '"' + TryClearStringValue(y) + '"'
                                        }
                                    }
                                };

                                listOfStyleAttributes.Add(styleAttribute);
                            }
                        }
                    }

                    stlyeValueAsJson = JsonConvert.SerializeObject(listOfStyleAttributes);
                }

                props.Add(new()
                {
                    Name  = "style",
                    Value = stlyeValueAsJson
                });

                return props;
            }

            if (project.ExportAsCSharpString)
            {
                List<FinalCssItem> listOFinalCssItems = [];
                {
                    foreach (var text in styles)
                    {
                        var designerStyleItem = CreateDesignerStyleItemFromText(project, text);
                        if (designerStyleItem.HasError)
                        {
                            return designerStyleItem.Error;
                        }

                        if (designerStyleItem.Value.Pseudo.HasValue())
                        {
                            return new NotSupportedException($"Pseudo styles are not supported in inline styles. {text}");
                        }

                        foreach (var finalCssItem in designerStyleItem.Value.FinalCssItems)
                        {
                            var finalCssItemResult = reCreateFinalCssItem(finalCssItem);
                            if (finalCssItemResult.HasError)
                            {
                                return finalCssItemResult.Error;
                            }

                            listOFinalCssItems.Add(finalCssItemResult.Value);
                        }
                    }
                }

                if (listOFinalCssItems.Count == 0)
                {
                    return props;
                }

                props.Add(new()
                {
                    Name  = "style",
                    Value = JsonConvert.SerializeObject(listOFinalCssItems)
                });
            }

            return props;

            Result<FinalCssItem> reCreateFinalCssItem(FinalCssItem finalCssItem)
            {
                return CreateFinalCssItem(new()
                {
                    Name = project.ExportAsCSharpString switch
                    {
                        true  => finalCssItem.Name,
                        false => KebabToCamelCase(finalCssItem.Name)
                    },
                    Value = finalCssItem.Value switch
                    {
                        null => null,

                        var y when y.StartsWith("request.") || y.StartsWith("context.") => y,

                        var y => project.ExportAsCSharpString switch
                        {
                            true  => TryClearStringValue(y),
                            false => '"' + TryClearStringValue(y) + '"'
                        }
                    }
                });
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