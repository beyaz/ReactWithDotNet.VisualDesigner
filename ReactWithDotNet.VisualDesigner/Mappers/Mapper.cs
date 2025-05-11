using ReactWithDotNet.VisualDesigner.DataAccess;
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
    
    public static ExportInput AsExportInput(this ComponentEntity input)
    {
        return new()
        {
            ProjectId     = input.ProjectId,
            ComponentId = input.Id,
            UserName      = input.UserName ?? Environment.UserName
        };
    }
    
    public static GetComponentDataInput Map(this ApplicationState state)
    {
        return new() { ComponentId = state.ComponentId, UserName = state.UserName };
    }
}