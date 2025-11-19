using System.IO;
using Newtonsoft.Json.Linq;

namespace ReactWithDotNet.VisualDesigner.TsxAnalyze;

static class SuggestionFromTsxCode
{
    public static async Task<Result<IReadOnlyList<string>>> GetBooleans(string tsxFilePath)
    {
        if (tsxFilePath is null || File.Exists(tsxFilePath))
        {
            return new List<string>();
        }

        var fileContent = await File.ReadAllTextAsync(tsxFilePath);

        var result = await NodeJsBridge.Ast(fileContent);
        if (result.HasError)
        {
            return result.Error;
        }

        var astAsJsonText = result.Value;

        var astObj = JObject.Parse(astAsJsonText);

        var analysisResult = TsxWalker.AnalyzeAst(astObj);

        return new List<string>
        {
            from x in analysisResult.States
            select x.State
        };
    }
}