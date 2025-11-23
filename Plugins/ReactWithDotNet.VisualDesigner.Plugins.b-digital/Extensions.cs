
global using static ReactWithDotNet.VisualDesigner.Plugins.Extensions;

namespace ReactWithDotNet.VisualDesigner.Plugins;

static class Extensions
{
    public static ReactNode AddContextProp(ReactNode node)
    {
        if (node.Properties.Any(p => p.Name == "context"))
        {
            return node;
        }

        return node with
        {
            Properties = node.Properties.Add(new()
            {
                Name  = "context",
                Value = "context"
            })
        };
    }
    
    public static ReactNode TryFindDesignNamedNode(this ReactNode node, string designName)
    {
        if (node.Properties.Any(p=>p.Name == Design.Name && TryClearStringValue(p.Value) == TryClearStringValue(designName)))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var namedNode = child.TryFindDesignNamedNode(designName);
            if (namedNode is not null)
            {
                return namedNode;
            }
        }

        return null;
    }
    
    public static Element TryFindDesignNamedElement(this Element element, string designName)
    {
        if (element is null)
        {
            return null;
        }
        if (element is HtmlElement htmlElement)
        {
            if (htmlElement.data.TryGetValue(Design.Name, out var name))
            {
                if (TryClearStringValue(name) == TryClearStringValue(designName))
                {
                    return element;
                }
            }
        }
        
        foreach (var child in element.children)
        {
            var namedElement = child.TryFindDesignNamedElement(designName);
            if (namedElement is not null)
            {
                return namedElement;
            }
        }

        return null;
    }
    
    public static ReactNode TryGetNodeItemAt(this ReactNode node, int[] location)
    {
        foreach (var childIndex in location)
        {
            if (node is null)
            {
                return null;
            }

            if (!(node.Children.Count > childIndex))
            {
                return null;
            }
                        
            node = node.Children[childIndex];
        }

        return node;
    }
}