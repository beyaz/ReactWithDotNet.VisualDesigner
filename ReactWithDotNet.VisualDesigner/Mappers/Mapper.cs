﻿using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Mappers;

static class Mapper
{
    public static ExportInput AsExportInput(this ApplicationState state)
    {
        return new()
        {
            ProjectId     = state.ProjectId,
            ComponentName = state.ComponentName,
            UserName      = state.UserName
        };
    }
    
    public static ExportInput AsExportInput(this ComponentEntity input)
    {
        return new()
        {
            ProjectId     = input.ProjectId,
            ComponentName = input.Name,
            UserName      = input.UserName ?? Environment.UserName
        };
    }
}