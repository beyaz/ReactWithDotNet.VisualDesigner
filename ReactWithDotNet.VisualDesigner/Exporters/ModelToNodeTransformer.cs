using System.Collections.Immutable;
using Newtonsoft.Json;

namespace ReactWithDotNet.VisualDesigner.Exporters;

static class ModelToNodeTransformer
{
    public static async Task<Result<ReactNode>> ConvertVisualElementModelToReactNodeModel(ProjectConfig project, VisualElementModel elementModel)
    {
        // Open tag
        var tag = elementModel.Tag;

        var node = new ReactNode
        {
            Tag = tag,

            HtmlElementType = TryGetHtmlElementTypeByTagName(tag)
        };

        // arrange inline styles
        {
            if (project.ExportStylesAsInline || project.ExportAsCSharp || project.ExportAsCSharpString)
            {
                // Transfer properties
                foreach (var property in elementModel.Properties)
                {
                    var propertyIsSuccessfullyParsed = false;

                    foreach (var parsedProperty in ParseProperty(property))
                    {
                        node = node with
                        {
                            Properties = node.Properties.Add(new()
                            {
                                Name  = parsedProperty.Name,
                                Value = parsedProperty.Value
                            })
                        };

                        propertyIsSuccessfullyParsed = true;
                    }

                    if (!propertyIsSuccessfullyParsed)
                    {
                        return new Exception($"PropertyParseError: {property}");
                    }
                }

                var result = convertStyleToInlineStyleObject(project, elementModel);
                if (result.HasError)
                {
                    return result.Error;
                }

                elementModel = result.Value.modifiedElementModel;

                var inlineStyle = result.Value.inlineStyle;

                if (inlineStyle.Any())
                {
                    var inlineStyleProperty = new ReactProperty
                    {
                        Name  = "style",
                        Value = "{" + string.Join(", ", inlineStyle.Select(x => $"{x.Name}: {x.Value}")) + "}"
                    };

                    if (project.ExportAsCSharp || project.ExportAsCSharpString)
                    {
                        inlineStyleProperty = inlineStyleProperty with
                        {
                            Value = JsonConvert.SerializeObject(inlineStyle)
                        };
                    }

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

                // Transfer properties
                foreach (var property in elementModel.Properties)
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

                    node = node with
                    {
                        Properties = node.Properties.Add(new()
                        {
                            Name  = parsedProperty.Value.Name,
                            Value = parsedProperty.Value.Value
                        })
                    };
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

                    node = node with
                    {
                        Properties = node.Properties.Add(new()
                        {
                            Name  = "className",
                            Value = firstLastChar + string.Join(" ", classNames) + firstLastChar
                        })
                    };
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
    }

    static Result<(VisualElementModel modifiedElementModel, IReadOnlyList<FinalCssItem> inlineStyle)>
        convertStyleToInlineStyleObject(ProjectConfig project, VisualElementModel elementModel)
    {
        var finalCssList =
            ListFrom(from text in elementModel.Styles
                     let item = CreateDesignerStyleItemFromText(project, text)
                     let finalCssItems = item switch
                     {
                         var x when x.HasError => [item.Error],

                         var x when x.Value.Pseudo is not null => [new NotSupportedException("Pseudo styles are not supported in inline styles.")],

                         _ => from x in item.Value.FinalCssItems
                             select CreateFinalCssItem
                                 (new()
                                  {
                                      Name = KebabToCamelCase(x.Name),

                                      Value = x.Value switch
                                      {
                                          null => null,

                                          var y when y.StartsWith("request.") || y.StartsWith("context.") => y,

                                          var y => '"' + TryClearStringValue(y) + '"'
                                      }
                                  }
                                 )
                     }
                     from x in finalCssItems
                     select x);

        if (finalCssList.HasError)
        {
            return finalCssList.Error;
        }

        return (elementModel with { Styles = [] }, finalCssList.Value);
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