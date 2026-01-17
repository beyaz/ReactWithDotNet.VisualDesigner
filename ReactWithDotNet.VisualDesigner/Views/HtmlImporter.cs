using System.Reflection;
using HtmlAgilityPack;

namespace ReactWithDotNet.VisualDesigner.Views;

static class HtmlImporter
{
    public static bool CanImportAsHtml(string html)
    {
        if (html is null)
        {
            return false;
        }

        if (html.StartsWith('<') && html.EndsWith('>') && html.Length > 4)
        {
            return true;
        }

        return false;
    }

    public static VisualElementModel ConvertToVisualElementModel(ProjectConfig project, string html)
    {
        var htmlDocument = new HtmlDocument
        {
            OptionOutputOriginalCase = true
        };
        htmlDocument.LoadHtml(html);

        return ConvertToVisualElementModel(project, htmlDocument.DocumentNode.FirstChild);
    }

    static IReadOnlyList<string> ArrangeAccordingToProject(ProjectConfig project, IReadOnlyList<string> designerStyles)
    {
        foreach (var (className, css) in project.Styles)
        {
            IReadOnlyDictionary<string, string> htmlStyleAttributes;
            {
                var (value, exception) = Style.ParseCssAsDictionary(css);
                if (exception is not null)
                {
                    continue;
                }

                htmlStyleAttributes = value;
            }
           

            if (hasFullMatch(project, designerStyles, htmlStyleAttributes))
            {
                foreach (var (htmlAttributeName, htmlAttributeValue) in htmlStyleAttributes)
                {
                    designerStyles = designerStyles.RemoveAll(x => hasMatch(project, x, htmlAttributeName, htmlAttributeValue));
                }

                designerStyles = designerStyles.Add(className);
            }
        }

        return designerStyles;

        static bool hasFullMatch(ProjectConfig project, IReadOnlyList<string> designerStyles, IReadOnlyDictionary<string, string> htmlStyleAttributes)
        {
            foreach (var (htmlAttributeName, htmlAttributeValue) in htmlStyleAttributes)
            {
                if (!designerStyles.Any(x => hasMatch(project, x, htmlAttributeName, htmlAttributeValue)))
                {
                    return false;
                }
            }

            return true;
        }

        static bool hasMatch(ProjectConfig project, string designerStyleText, string htmlAttributeName, string htmlAttributeValue)
        {
            foreach (var designerStyleItem in TryCreateDesignerStyleItemFromText(project, designerStyleText))
            {
                if (designerStyleItem.FinalCssItems.Count == 1)
                {
                    foreach (var finalCssItem in designerStyleItem.FinalCssItems)
                    {
                        if (finalCssItem.Name == htmlAttributeName && TryClearStringValue(finalCssItem.Value) == TryClearStringValue(htmlAttributeValue))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    static VisualElementModel ConvertToVisualElementModel(ProjectConfig project, HtmlNode htmlNode)
    {
        if (htmlNode is null)
        {
            return null;
        }

        if (htmlNode.NodeType == HtmlNodeType.Comment)
        {
            return null;
        }

        if (htmlNode.NodeType == HtmlNodeType.Text)
        {
            return null;
        }

        var model = new VisualElementModel
        {
            Tag = htmlNode.Name
        };

        HtmlElement element = null;
        {
            TryGetHtmlElementTypeByTagName(model.Tag).HasValue(elementType => { element = (HtmlElement)Activator.CreateInstance(elementType); });
        }

        foreach (var attribute in htmlNode.Attributes)
        {
            var attributeName = attribute.Name;
            var attributeValue = attribute.Value;

            if (attributeName == "style")
            {
                var (map, _) = Style.ParseCssAsDictionary(attributeValue);
                if (map != null)
                {
                    map = tryConvertOutlineToBorder(map);
                    map = tryMergeForShorthandDeclarations(map);
                        
                    foreach (var item in map.Where(skipWordWrap).Where(skipJustifyContentFlexStart))
                    {
                        model = model with { Styles = model.Styles.Add(item.Key + ": " + item.Value) };
                    }
                }

                continue;
            }

            if (attributeName == "class")
            {
                attributeName = "className";

                var listOfCssClass = attributeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                List<string> remainingClassNames = [];

                foreach (var (className, styles) in from className in listOfCssClass select processClassName(project, className))
                {
                    model = model with { Styles = model.Styles.AddRange(styles) };

                    className.HasValue(n => remainingClassNames.Add(n));
                }

                if (remainingClassNames.Count == 0)
                {
                    continue;
                }

                attributeValue = string.Join(" ", remainingClassNames);

                static (Maybe<string> className, IReadOnlyList<string> styles) processClassName(ProjectConfig project, string className)
                {
                    var designerStyleItem = TryConvertTailwindUtilityClassToHtmlStyle(project, className);
                    if (!designerStyleItem.HasError)
                    {
                        var returnStyles = new List<string>();

                        var pseudo = designerStyleItem.Value.Pseudo;
                        var styles = designerStyleItem.Value.FinalCssItems;

                        foreach (var finalCssItem in styles)
                        {
                            if (pseudo.HasValue)
                            {
                                returnStyles.Add(pseudo + ":" + finalCssItem.Name + ": " + finalCssItem.Value);
                            }
                            else
                            {
                                returnStyles.Add(finalCssItem.Name + ": " + finalCssItem.Value);
                            }
                        }

                        return (None, returnStyles);
                    }

                    return (className, []);
                }
            }

            if (element is not null)
            {
                var propertyInfo = element.GetType().GetProperty(attributeName.Replace("-", ""), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo is not null)
                {
                    attributeName = propertyInfo.Name;

                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        attributeValue = "'" + attributeValue + "'";
                    }
                }
            }

            model = model with { Properties = model.Properties.Add(attributeName + ": " + attributeValue) };
        }

        model = model with { Styles = ArrangeAccordingToProject(project, model.Styles) };

        if (htmlNode.ChildNodes.Count == 1 && htmlNode.ChildNodes[0].NodeType == HtmlNodeType.Text && htmlNode.ChildNodes[0].InnerText.HasValue)
        {
            model = model with { Properties = model.Properties.Add($"{Design.Content}: '{htmlNode.ChildNodes[0].InnerText.Trim()}'") };
            model = model with { Properties = model.Properties.Add($"{Design.ContentPreview}: '{htmlNode.ChildNodes[0].InnerText.Trim()}'") };

            return model;
        }

        foreach (var child in htmlNode.ChildNodes)
        {
            var childModel = ConvertToVisualElementModel(project, child);
            if (childModel is null)
            {
                continue;
            }

            model = model with { Children = model.Children.Add(childModel) };
        }

        return model;

        static bool skipWordWrap(KeyValuePair<string, string> htmlStyleAttribute)
        {
            if (htmlStyleAttribute is { Key: "word-wrap", Value: "break-word" })
            {
                return false;
            }

            return true;
        }

        static bool skipJustifyContentFlexStart(KeyValuePair<string, string> htmlStyleAttribute)
        {
            if (htmlStyleAttribute is { Key: "justify-content", Value: "flex-start" })
            {
                return false;
            }

            return true;
        }

        IReadOnlyDictionary<string, string> tryConvertOutlineToBorder(IReadOnlyDictionary<string, string> htmlStyleAttributes)
        {
            if (htmlStyleAttributes.TryGetValue("outline", out var outline))
            {
                if (htmlStyleAttributes.TryGetValue("outline-offset", out var outlineOffset))
                {
                    if (outlineOffset == "-1px" && !htmlStyleAttributes.ContainsKey("border"))
                    {
                        var clone = htmlStyleAttributes.ToDictionary();
                        
                        clone.Remove("outline");
                        clone.Remove("outline-offset");
                        
                        clone.Add("border", outline);

                        return clone;
                    }
                }   
            }

            return htmlStyleAttributes;
        }
       

        static IReadOnlyDictionary<string, string> tryMergeForShorthandDeclarations(IReadOnlyDictionary<string, string> inlineStyleMap)
        {
            foreach (var func in new[] { fixPaddingAndMargin })
            {
                inlineStyleMap = func(inlineStyleMap);
            }

            return inlineStyleMap;

            static IReadOnlyDictionary<string, string> fixPaddingAndMargin(IReadOnlyDictionary<string, string> inlineStyleMap)
            {
                var map = new Dictionary<string, string>(inlineStyleMap);

                foreach (var prefix in new[] { "padding", "margin" })
                {
                    map.TryGetValue(prefix, out var value);

                    map.TryGetValue($"{prefix}-top", out var top);
                    map.TryGetValue($"{prefix}-bottom", out var bottom);

                    map.TryGetValue($"{prefix}-left", out var left);
                    map.TryGetValue($"{prefix}-right", out var right);

                    if (value is null)
                    {
                        if (top.HasValue && top == bottom)
                        {
                            if (left.HasValue && left == right)
                            {
                                if (left == top)
                                {
                                    map.RemoveAll($"{prefix}-top", $"{prefix}-bottom", $"{prefix}-left", $"{prefix}-right");
                                    map.Add(prefix, left);
                                }
                                else
                                {
                                    map.RemoveAll($"{prefix}-top", $"{prefix}-bottom", $"{prefix}-left", $"{prefix}-right");
                                    map.Add(prefix, $"{top} {right}");
                                }
                            }
                        }
                    }
                }

                return map;
            }
        }
    }
}