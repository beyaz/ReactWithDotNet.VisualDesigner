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

                    foreach (var (name, value) in TryParseProperty(property))
                    {
                        node = node with
                        {
                            Properties = node.Properties.Add(new()
                            {
                                Name  = name,
                                Value = value
                            })
                        };

                        propertyIsSuccessfullyParsed = true;
                    }

                    if (!propertyIsSuccessfullyParsed)
                    {
                        return new Exception($"PropertyParseError: {property}");
                    }
                }

                var result = convertStyleToInlineStyleObject(elementModel);
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
                    string name, value;
                    {
                        var parseResult = TryParseProperty(property);
                        if (parseResult.HasNoValue)
                        {
                            return new Exception($"PropertyParseError: {property}");
                        }

                        name  = parseResult.Value.Name;
                        value = parseResult.Value.Value;
                    }

                    if (name == "class")
                    {
                        classNames.AddRange(value.Split(" ", StringSplitOptions.RemoveEmptyEntries));
                        continue;
                    }

                    node = node with { Properties = node.Properties.Add(new() { Name = name, Value = value }) };
                }

                // todo : optimize styles, for example width and height to size
                {
                    var width = FirstOrDefaultOf
                        (from x in elementModel.Styles.Select((text, index) => new { text, index })
                         let styleItem = ParseStyleAttribute(x.text)
                         where styleItem.Name == "width"
                         select new
                         {
                             styleItem.Value,
                             x.index
                         });
                    
                    var height = FirstOrDefaultOf
                        (from x in elementModel.Styles.Select((text, index) => new { text, index })
                         let styleItem = ParseStyleAttribute(x.text)
                         where styleItem.Name == "height"
                         select new
                         {
                             styleItem.Value,
                             x.index
                         });
                    
                    var size = FirstOrDefaultOf
                        (from x in elementModel.Styles.Select((text, index) => new { text, index })
                         let styleItem = ParseStyleAttribute(x.text)
                         where styleItem.Name == "size"
                         select new
                         {
                             styleItem.Value,
                             x.index
                         });
                    

                    if (width is not null && height is not null && width.Value == height.Value && height.Value.EndsWith("px") && size is null)
                    {
                        // todo: ??
                        //elementModel = elementModel with
                        //{
                        //    Styles = elementModel.Styles.SetItem(width.index, $"size: {width.Value}").RemoveAt(height.index)
                        //};
                    }
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
    }

    static IReadOnlyList<StyleAttribute> convertDesignerStyleItemsToStyleAttributes(IReadOnlyList<string> designerStyleItems)
    {
        return (from text in designerStyleItems select process(ParseStyleAttribute(text))).ToList();

        static StyleAttribute process(StyleAttribute styleAttribute)
        {
            var name = KebabToCamelCase(styleAttribute.Name);

            var value = styleAttribute.Value;
            {
                if (name == nameof(Style.fontWeight))
                {
                    value = tryGetFontWeight(value);
                }

                if (nameof(Style.gridRow).Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    nameof(Style.gridColumn).Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    nameof(Style.zIndex).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out _))
                    {
                        return styleAttribute with { Name = name, Value = value };
                    }
                }

                if (double.TryParse(value, out var valueAsDouble))
                {
                    value = valueAsDouble.AsPixel();
                }

                if (value?.StartsWith("request.") is true || value?.StartsWith("context.") is true)
                {
                    value = TryClearStringValue(value);
                }
                else
                {
                    value = '"' + TryClearStringValue(value) + '"';
                }
            }

            return styleAttribute with { Name = name, Value = value };
        }

        static string tryGetFontWeight(string weight)
        {
            if (!int.TryParse(weight, out var numericWeight))
            {
                return weight;
            }

            return numericWeight switch
            {
                100 => "thin",
                200 => "extra-light",
                300 => "light",
                400 => "normal",
                500 => "medium",
                600 => "semi-bold",
                700 => "bold",
                800 => "extra-bold",
                900 => "black",
                _   => weight
            };
        }
    }

    static Result<(VisualElementModel modifiedElementModel, IReadOnlyList<StyleAttribute> inlineStyle)>
        convertStyleToInlineStyleObject(VisualElementModel elementModel)
    {
        var styles = convertDesignerStyleItemsToStyleAttributes(elementModel.Styles);

        return (elementModel with { Styles = [] }, styles);
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