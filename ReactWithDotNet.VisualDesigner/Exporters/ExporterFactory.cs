global using  ExportInput = (int ProjectId,int ComponentId,string UserName );
global using  ExportOutput = (bool HasChange,  Toolbox.FileModel File );

namespace ReactWithDotNet.VisualDesigner.Exporters;

public sealed record SourceLinePoints(int LeftPaddingCount, int FirstReturnLineIndex, int FirstReturnCloseLineIndex);

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
    public static async Task<Result<string>> CalculateElementSourceCode(int projectId, IReadOnlyDictionary<string, string> componentConfig, VisualElementModel visualElement)
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
        
        if (project.ExportAsCSharpString)
        {
            return await CSharpStringExporter.CalculateElementTsxCode(projectId, componentConfig, visualElement);
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