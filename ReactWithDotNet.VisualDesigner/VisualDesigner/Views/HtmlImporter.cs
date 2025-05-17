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

    public static VisualElementModel ConvertToVisualElementModel(int projectId, string html)
    {
        var htmlDocument = new HtmlDocument
        {
            OptionOutputOriginalCase = true
        };
        htmlDocument.LoadHtml(html);

        return ConvertToVisualElementModel(projectId, htmlDocument.DocumentNode.FirstChild);
    }

    static VisualElementModel ConvertToVisualElementModel(int projectId, HtmlNode htmlNode)
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

        foreach (var attribute in htmlNode.Attributes)
        {
            var attributeName = attribute.Name;
            var attributeValue = attribute.Value;

            if (attributeName == "style")
            {
                var (map, _) = Style.ParseCssAsDictionary(attributeValue);
                if (map != null)
                {
                    foreach (var item in map)
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

                foreach (var className in listOfCssClass)
                {
                    processClassName(projectId, className).Then((x, y) =>
                    {
                        if (x.HasValue())
                        {
                            remainigClassNames.Add(x);
                        }

                        model.Styles.AddRange(y);
                    });
                }

                if (remainigClassNames.Count == 0)
                {
                    continue;
                }

                attributeValue = string.Join(" ", remainigClassNames);
                
                
                static (string className, IReadOnlyList<string> styles) processClassName(int projectId, string className)
                {
                    foreach (var designerStyleItem in TryConvertCssUtilityClassToHtmlStyle(projectId, className))
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

                        return (null, returnStyles);
                    }

                    return (className, []);
                }
            }

            model.Properties.Add(attributeName + ": " + attributeValue);
        }

        if (htmlNode.ChildNodes.Count == 1 && htmlNode.ChildNodes[0].NodeType == HtmlNodeType.Text && htmlNode.ChildNodes[0].InnerText.HasValue())
        {
            model.Properties.Add($"-text: '{htmlNode.ChildNodes[0].InnerText.Trim()}'");

            return model;
        }

        foreach (var child in htmlNode.ChildNodes)
        {
            var childModel = ConvertToVisualElementModel(projectId, child);
            if (childModel is null)
            {
                continue;
            }

            model.Children.Add(childModel);
        }

        return model;
    }
}