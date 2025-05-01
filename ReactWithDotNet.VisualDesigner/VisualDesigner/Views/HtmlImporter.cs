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
            Tag        = htmlNode.Name,
            Properties = htmlNode.Attributes.Select(a => a.Name + ": " + a.Value).ToList()
        };

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