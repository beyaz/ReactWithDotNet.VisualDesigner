namespace ReactWithDotNet.VisualDesigner.Exporters;

sealed record ExportOutput
{
    public bool HasChange { get; init; }
}