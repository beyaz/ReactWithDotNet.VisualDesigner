namespace ReactWithDotNet.VisualDesigner.Exporters;

public sealed record SourceLinePoints(int LeftPaddingCount, int FirstReturnLineIndex, int FirstReturnCloseLineIndex);

public sealed record ExportInput
{
    public required string UserName { get; init; }
}

static class ExporterFactory
{
    public static async Task<Result<(bool HasChange,FileModel File)>> ExportToFileSystem(ComponentScope componentScope, ExportInput input)
    {
        var project = componentScope.ProjectConfig;
       
        if (project.ExportAsCSharp)
        {
            return await CSharpExporter.ExportToFileSystem(componentScope, input);
        }

        if (project.ExportAsCSharpString)
        {
            return await CSharpStringExporter.ExportToFileSystem( componentScope,input);
        }
        
        return await TsxExporter.ExportToFileSystem(componentScope, input);
    }

    public static Result<SourceLinePoints> GetComponentLineIndexPointsInSourceFile(int projectId, IReadOnlyList<string> fileContent, string targetComponentName)
    {
        var project = GetProjectConfig(projectId);
        if (project is null)
        {
            return new ArgumentNullException($"ProjectNotFound. {projectId}");
        }
        
        if (project.ExportAsCSharp )
        {
            return CSharpExporter.GetComponentLineIndexPointsInCSharpFile(fileContent, targetComponentName);
        }
        
        if (project.ExportAsCSharpString)
        {
            return CSharpStringExporter.GetComponentLineIndexPointsInCSharpFile(fileContent, targetComponentName);
        }
        
        return TsxExporter.GetComponentLineIndexPointsInTsxFile(fileContent, targetComponentName);
    }
    public static  Task<Result<string>> CalculateElementSourceCode(ComponentScope componentScope, int componentId,int projectId, ComponentConfig componentConfig, VisualElementModel visualElement)
    {
        var project = GetProjectConfig(projectId);
        
        if (project.ExportAsCSharp)
        {
            return  CSharpExporter.CalculateElementTsxCode(componentScope, visualElement);
        }
        
        if (project.ExportAsCSharpString)
        {
            return  CSharpStringExporter.CalculateElementTsxCode(componentScope, visualElement);
        }
        
        return from tsCode in TsxExporter.CalculateElementTsxCode(componentScope, visualElement)
               from formattedTsCode in NodeJsBridge.FormatCode(tsCode, project.PrettierOptions)
               select formattedTsCode;
    }
}