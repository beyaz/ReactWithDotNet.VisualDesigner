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

    public static Result<(int componentDeclarationLineIndex, int leftPaddingCount, int firstReturnLineIndex, int firstReturnCloseLineIndex)> GetComponentLineIndexPointsInSourceFile(int projectId, IReadOnlyList<string> fileContent, string targetComponentName)
    {
        var project = GetProjectConfig(projectId);
        if (project is null)
        {
            return new ArgumentNullException($"ProjectNotFound. {projectId}");
        }
        
        if (project.ExportAsCSharp)
        {
            return CSharpExporter.GetComponentLineIndexPointsInCSharpFile(fileContent, targetComponentName);
        }
        
        return TsxExporter.GetComponentLineIndexPointsInTsxFile(fileContent, targetComponentName);
    }
    public static async Task<Result<string>> CalculateElementTsxCode(int projectId, IReadOnlyDictionary<string, string> componentConfig, VisualElementModel visualElement)
    {
        var project = GetProjectConfig(projectId);
        if (project is null)
        {
            return new ArgumentNullException($"ProjectNotFound. {projectId}");
        }
        
        if (project.ExportAsCSharp)
        {
            return await CSharpExporter.CalculateElementTsxCode(projectId, componentConfig, visualElement);
        }

        string tsxCode;
        {
            var result =  await TsxExporter.CalculateElementTsxCode(projectId, componentConfig, visualElement);
            if (result.HasError)
            {
                return result.Error;
            }
        
            result = await Prettier.FormatCode(result.Value);
            if (result.HasError)
            {
                return result.Error.Message;
            }

            tsxCode = result.Value;
        }

        return tsxCode;
    }
}