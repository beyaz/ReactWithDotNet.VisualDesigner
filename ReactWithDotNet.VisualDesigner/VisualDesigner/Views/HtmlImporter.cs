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

    public static VisualElementModel ConvertToVisualElementModel(string html)
    {
        var htmlDocument = new HtmlDocument
        {
            OptionOutputOriginalCase = true
        };
        htmlDocument.LoadHtml(html);

        return ConvertToVisualElementModel(htmlDocument.DocumentNode.FirstChild);
    }

    static VisualElementModel ConvertToVisualElementModel(HtmlNode htmlNode)
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
                    var maybe = CssHelper.TryConvertCssUtilityClassToHtmlStyle(className);
                    if (maybe.HasValue)
                    {
                        var pseudo = maybe.Value.Pseudo;
                        var styles = maybe.Value.CssStyles;
                        
                        foreach (var (name, value) in styles)
                        {
                            if (pseudo.HasValue())
                            {
                                model.Styles.Add(pseudo +":"+ name + ": " + value);
                            }
                            else
                            {
                                model.Styles.Add(name + ": " + value);
                            }
                        }
                        
                        continue;
                    }
                    
                    remainigClassNames.Add(className);
                }

                if (remainigClassNames.Count == 0)
                {
                    continue;
                }

                attributeValue = string.Join(" ", remainigClassNames);
            }

            model.Properties.Add(attributeName + ": " + attributeValue);
        }

        if (htmlNode.ChildNodes.Count == 1 && htmlNode.ChildNodes[0].NodeType == HtmlNodeType.Text && htmlNode.ChildNodes[0].InnerText.HasValue())
        {
            model.Text = htmlNode.ChildNodes[0].InnerText.Trim();

            return model;
        }

        foreach (var child in htmlNode.ChildNodes)
        {
            var childModel = ConvertToVisualElementModel(child);
            if (childModel is null)
            {
                continue;
            }

            model.Children.Add(childModel);
        }

        return model;
    }
}