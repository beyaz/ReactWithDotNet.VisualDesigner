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

    public static Element FindElement(this Element element, Predicate<Element> predicate)
    {
        if (element is null)
        {
            return null;
        }

        if (predicate(element))
        {
            return element;
        }

        foreach (var child in element.children)
        {
            var foundElement = child.FindElement(predicate);
            if (foundElement is not null)
            {
                return foundElement;
            }
        }

        return null;
    }

    public static Element FindElement(this ElementCollection elementCollection, Predicate<Element> predicate)
    {
        if (elementCollection is null)
        {
            return null;
        }

        foreach (var child in elementCollection)
        {
            var foundElement = child.FindElement(predicate);
            if (foundElement is not null)
            {
                return foundElement;
            }
        }

        return null;
    }

    public static Element FindElementByElementType(this ElementCollection element, Type elementType)
    {
        return element.FindElement(HasMatch);

        bool HasMatch(Element el)
        {
            if (el?.GetType() == elementType)
            {
                return true;
            }

            return false;
        }
    }

    public static ReactNode FindNodeByTag(this ReactNode node, string tagName)
    {
        if (node is null)
        {
            return null;
        }

        if (node.Tag == tagName)
        {
            return node;
        }

        foreach (var childNode in node.Children)
        {
            var nodeMaybeFound = childNode.FindNodeByTag(tagName);
            if (nodeMaybeFound is not null)
            {
                return nodeMaybeFound;
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