namespace ReactWithDotNet.VisualDesigner.JsxPreview;

sealed class JsxElementDto
{
    // @formatter:off
        
    public string Tag { get; set; }
    
    public List<string> Props { get; set; } = [];
    
    public List<JsxElementDto> Children { get; set; } = [];
    
    public string Condition { get; set; }
    
    // @formatter:on
}

sealed class MethodResult
{
    // @formatter:off
      
    public string MethodName { get; set; }
      
    public List<JsxElementDto> Elements { get; set; } = [];

    // @formatter:on
}