global using  ExportInput = (int ProjectId,int ComponentId,string UserName );

namespace ReactWithDotNet.VisualDesigner.Exporters;

public sealed record SourceLinePoints(int LeftPaddingCount, int FirstReturnLineIndex, int FirstReturnCloseLineIndex);

static class ExporterFactory
{
    public static async Task<Result<(bool HasChange,FileModel File)>> ExportToFileSystem(ExportInput input)
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

        if (project.ExportAsCSharpString)
        {
            return await CSharpStringExporter.ExportToFileSystem(input);
        }
        
        return await TsxExporter.ExportToFileSystem(input);
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
    public static  Task<Result<string>> CalculateElementSourceCode(int projectId, ComponentConfig componentConfig, VisualElementModel visualElement)
    {
        var project = GetProjectConfig(projectId);
        
        if (project.ExportAsCSharp)
        {
            return  CSharpExporter.CalculateElementTsxCode(projectId, componentConfig, visualElement);
        }
        
        if (project.ExportAsCSharpString)
        {
            return  CSharpStringExporter.CalculateElementTsxCode(projectId, componentConfig, visualElement);
        }
        
        return from tsCode in TsxExporter.CalculateElementTsxCode(projectId, componentConfig, visualElement)
               from formattedTsCode in Prettier.FormatCode(tsCode, new (){ TabWidth = project.TabWidth})
               select formattedTsCode;
    }
}