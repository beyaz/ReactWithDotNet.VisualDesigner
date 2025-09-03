namespace ReactWithDotNet.VisualDesigner.Exporters;

static class ExporterFactory
{
    public static async Task<Result<ExportOutput>> ExportToFileSystem(ExportInput input)
    {
        var project = GetProjectConfig(input.ProjectId);
        if (project is null)
        {
            return new ArgumentNullException($"ProjectNotFound. {input.ProjectId}");
        }
        
        if (project.ExportAsCSharp)
        {
            return await CSharpExporter.ExportToFileSystem(input);
        }

        return await TsxExporter.ExportToFileSystem(input);
    }
}