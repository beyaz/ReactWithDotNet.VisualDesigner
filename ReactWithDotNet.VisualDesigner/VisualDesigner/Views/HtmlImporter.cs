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
            var name = attribute.Name;
            var value = attribute.Value;
            
            if (name == "style")
            {
                continue;
            }

            if (name == "class")
            {
                name = "className";
            }
            
            model.Properties.Add(name + ": " + value);
        }
        

        var css = htmlNode.Attributes.FirstOrDefault(p => p.Name == "style")?.Value;
        if (css.HasValue())
        {
            var (map, _) = Style.ParseCssAsDictionary(css);
            if (map!=null)
            {
                foreach (var (key, value) in map)
                {
                    model.Styles.Add(key + ": " + value);
                }
            }
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