namespace ReactWithDotNet.VisualDesigner.Exporters;

sealed record ExportInput
{
    // @formatter:off
     
    public required int ProjectId { get; init; }
    
    public required int ComponentId { get; init; }
    
    public required string UserName { get; init; }
    
    public void Deconstruct(out int projectId, out int componentId,  out string userName)
    {
        projectId     = ProjectId;
        
        componentId     = ComponentId;
        
        userName      = UserName;
    }
    
    // @formatter:on
}

sealed record ExportOutput
{
    public bool HasChange { get; init; }
}