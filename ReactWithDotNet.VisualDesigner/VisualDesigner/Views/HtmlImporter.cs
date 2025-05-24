using System.Reflection;
using HtmlAgilityPack;

namespace ReactWithDotNet.VisualDesigner;

static class HtmlImporter
{
    public static bool CanImportAsHtml(string html)
    {
        if (html is null)
        {
            return false;
        }

        if (html.StartsWith("<") && html.EndsWith(">") && html.Length > 4)
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

    static void ArrangeAccordingToProject(ProjectConfig project, List<string> designerStyles)
    {
        foreach (var (className, css) in project.Styles)
        {
            var response = Style.ParseCssAsDictionary(css);
            if (response.exception is not null)
            {
                continue;
            }

            var htmlStyleAttributes = response.value;

            if (hasFullMatch(project, designerStyles, htmlStyleAttributes))
            {
                foreach (var (htmlAttributeName, htmlAttributeValue) in htmlStyleAttributes)
                {
                    designerStyles.RemoveAll(x => hasMatch(project, x, htmlAttributeName, htmlAttributeValue));
                }

                designerStyles.Add(className);
            }
        }

        return;

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
            foreach (var designerStyleItem in CreateDesignerStyleItemFromText(project, designerStyleText))
            {
                if (designerStyleItem.RawHtmlStyles.Count == 1)
                {
                    foreach (var (key, value) in designerStyleItem.RawHtmlStyles)
                    {
                        if (key == htmlAttributeName && TryClearStringValue(value) == TryClearStringValue(htmlAttributeValue))
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
                    foreach (var item in tryMergeForShorthandDeclerations(map).Where(skipWordWrap).Where(skipJustifyContentFlexStart))
                    {
                        model.Styles.Add(item.Key + ": " + item.Value);
                    }
                }

                continue;
            }

            if (attributeName == "class")
            {
                attributeName = "className";

                var listOfCssClass = attributeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                List<string> remainigClassNames = [];

                foreach (var (className, styles) in from className in listOfCssClass select processClassName(project, className))
                {
                    model.Styles.AddRange(styles);

                    className.HasValue(n => remainigClassNames.Add(n));
                }

                if (remainigClassNames.Count == 0)
                {
                    continue;
                }

                attributeValue = string.Join(" ", remainigClassNames);

                static (Maybe<string> className, IReadOnlyList<string> styles) processClassName(ProjectConfig project, string className)
                {
                    foreach (var designerStyleItem in TryConvertCssUtilityClassToHtmlStyle(project, className))
                    {
                        var returnStyles = new List<string>();

                        var pseudo = designerStyleItem.Pseudo;
                        var styles = designerStyleItem.RawHtmlStyles;

                        foreach (var (name, value) in styles)
                        {
                            if (pseudo.HasValue())
                            {
                                returnStyles.Add(pseudo + ":" + name + ": " + value);
                            }
                            else
                            {
                                returnStyles.Add(name + ": " + value);
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

            model.Properties.Add(attributeName + ": " + attributeValue);
        }

        ArrangeAccordingToProject(project, model.Styles);

        if (htmlNode.ChildNodes.Count == 1 && htmlNode.ChildNodes[0].NodeType == HtmlNodeType.Text && htmlNode.ChildNodes[0].InnerText.HasValue())
        {
            model.Properties.Add($"-text: '{htmlNode.ChildNodes[0].InnerText.Trim()}'");

            return model;
        }

        foreach (var child in htmlNode.ChildNodes)
        {
            var childModel = ConvertToVisualElementModel(project, child);
            if (childModel is null)
            {
                continue;
            }

            model.Children.Add(childModel);
        }

        return model;

        static bool skipWordWrap(KeyValuePair<string, string> htmlStyleAttribute)
        {
            if (htmlStyleAttribute.Key == "word-wrap" && htmlStyleAttribute.Value == "break-word")
            {
                return false;
            }

            return true;
        }

        static bool skipJustifyContentFlexStart(KeyValuePair<string, string> htmlStyleAttribute)
        {
            if (htmlStyleAttribute.Key == "justify-content" && htmlStyleAttribute.Value == "flex-start")
            {
                return false;
            }

            return true;
        }

        static IReadOnlyDictionary<string, string> tryMergeForShorthandDeclerations(IReadOnlyDictionary<string, string> inlineStyleMap)
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
                        if (top.HasValue() && top == bottom)
                        {
                            if (left.HasValue() && left == right)
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