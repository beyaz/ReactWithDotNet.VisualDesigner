using System.Collections.Immutable;
using Newtonsoft.Json;

namespace ReactWithDotNet.VisualDesigner.Exporters;

static class ModelToNodeTransformer
{
    public static async Task<Result<ReactNode>> ConvertVisualElementModelToReactNodeModel(ProjectConfig project, VisualElementModel elementModel)
    {
        // Open tag

        var node = new ReactNode
        {
            Tag = elementModel.Tag,

            HtmlElementType = TryGetHtmlElementTypeByTagName(elementModel.Tag)
        };

        // calculate properties
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

            node = node with
            {
                Properties = props.Value.ToImmutableList()
            };
        }

        var hasNoChildAndHasNoText = elementModel.Children.Count == 0 && elementModel.HasNoText();
        if (hasNoChildAndHasNoText)
        {
            return node;
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

        return node with
        {
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

            var finalCssList = ListFrom(from text in styles
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

            var inlineStyle = finalCssList.Value;

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

                props.Add(inlineStyleProperty);
            }

            return props;
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