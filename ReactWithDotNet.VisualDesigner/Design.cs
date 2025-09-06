namespace ReactWithDotNet.VisualDesigner;

static class Design
{
    public const string Text = "d-text";
    
    public const string TextPreview = "d-text-preview";
    
    public const string SpreadOperator = "--spreadOperator";
    
    public const string Src = "d-src";
    
    public const string ItemsSource = "d-items-source";
    
    public const string ItemsSourceDesignTimeCount = "d-items-source-design-time-count";
    
    public const string HideIf = "d-hide-if";
    
    public const string ShowIf = "d-show-if";
    
    public const string IsImportedChild = "d-is-imported-child";
    
    public const string Name = "d-name";

    public static bool IsDesignTimeName(string name)
    {
        if (name is null)
        {
            return false;
        }
        
        return name.StartsWith("d-", StringComparison.OrdinalIgnoreCase);
    }

}

static class TextNode
{
    public const string Tag = "#text";
}