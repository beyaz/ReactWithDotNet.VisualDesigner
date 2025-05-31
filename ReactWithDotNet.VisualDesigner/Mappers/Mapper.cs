using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Mappers;

static class Mapper
{
    public static ExportInput AsExportInput(this ApplicationState state)
    {
        return new()
        {
            ProjectId     = state.ProjectId,
            ComponentId = state.ComponentId,
            UserName      = state.UserName
        };
    }
    
    
    public static GetComponentDataInput Map(this ApplicationState state)
    {
        return new() { ComponentId = state.ComponentId, UserName = state.UserName };
    }
}