namespace ReactWithDotNet.VisualDesigner.JsxPreview;

sealed class JsxElementDto
{
    // @formatter:off
        
    public string Tag { get; set; }
    
    public IReadOnlyList<string> Properties { get; set; } = [];
    
    public List<JsxElementDto> Children { get; set; } = [];
    
    
    // @formatter:on
}

sealed class MethodResult
{
    // @formatter:off
      
    public string MethodName { get; set; }
      
    public JsxElementDto RootElement { get; set; }

    // @formatter:on
}