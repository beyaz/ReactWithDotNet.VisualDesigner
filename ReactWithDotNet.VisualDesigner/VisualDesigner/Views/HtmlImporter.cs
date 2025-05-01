using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace ReactWithDotNet.VisualDesigner;

static class HtmlImporter
{
    public static VisualElementModel Import(string html)
    {
        var htmlDocument = new HtmlDocument
        {
            OptionOutputOriginalCase = true
        };
        htmlDocument.LoadHtml(html);

        var root = htmlDocument.DocumentNode.FirstChild;
        
        
        var sb = new StringBuilder();

        root.WriteTo(new StringWriter(sb));

        sb.ToString();
        
        var model = new VisualElementModel
        {
            Tag         = root.Name,
            Text        = root.InnerText,
            Properties  = root.Attributes.Select(a => a.Name).ToList(),
            Children    = new()
        };

        foreach (var child in root.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Element)
            {
                model.Children.Add(Import(child.OuterHtml));
            }
        }

        return model;
    }
}