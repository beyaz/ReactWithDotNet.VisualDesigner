
global using  ExportInput = (int ProjectId,int ComponentId,string UserName );

namespace ReactWithDotNet.VisualDesigner.Exporters;

sealed record ExportOutput
{
    public bool HasChange { get; init; }
}