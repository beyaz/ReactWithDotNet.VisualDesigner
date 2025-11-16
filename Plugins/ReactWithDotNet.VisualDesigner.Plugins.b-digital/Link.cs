namespace ReactWithDotNet.VisualDesigner.Plugins;

[CustomComponent]
public sealed class Link : PluginComponentBase
{
    public string className { get; set; }
    public string href { get; set; }
    public string target { get; set; }

    protected override Element render()
    {
        return new a
        {
            href      = href,
            target    = target,
            className = className
        };
    }
            
    [TryGetIconForElementTreeNode]
    public static Scope TryGetIconForElementTreeNode(Scope scope)
    {
        var model = Plugin.VisualElementModel[scope];
        
        if (model.Tag == nameof(Link))
        {
            return Scope.Create(new()
            {
                { Plugin.IconForElementTreeNode, new IconLink() }
            });
        }

        return Scope.Empty;
    }
}