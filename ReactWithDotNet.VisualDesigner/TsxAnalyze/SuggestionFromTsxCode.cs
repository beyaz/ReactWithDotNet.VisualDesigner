using System.IO;
using System.Text.RegularExpressions;

namespace ReactWithDotNet.VisualDesigner.TsxAnalyze;

static class SuggestionFromTsxCode
{
    public static async Task<Result<IReadOnlyList<string>>> GetAllVariableSuggestionsInFile(string tsxFilePath)
    {
        if (tsxFilePath is null || !File.Exists(tsxFilePath))
        {
            return new List<string>();
        }

        var fileContent = await File.ReadAllTextAsync(tsxFilePath);

        return Result.From(CalculateVariableSuggestions(fileContent));
    }

    static IReadOnlyList<string> CalculateVariableSuggestions(string tsxCode)
    {
        var suggestions = new HashSet<string>();

        // Fonksiyon isimleri
        const string functionPattern = @"(?:function\s+([A-Za-z_]\w*)|const\s+([A-Za-z_]\w*)\s*=\s*\()";
        foreach (Match match in Regex.Matches(tsxCode, functionPattern))
        {
            if (match.Groups[1].Success)
            {
                suggestions.Add(match.Groups[1].Value);
            }

            if (match.Groups[2].Success)
            {
                suggestions.Add(match.Groups[2].Value);
            }
        }

        // Variable isimleri
        const string variablePattern = @"(?:let|const|var)\s+([A-Za-z_]\w*)";
        foreach (Match match in Regex.Matches(tsxCode, variablePattern))
        {
            suggestions.Add(match.Groups[1].Value);
        }

        // useState hook isimleri
        const string useStatePattern = @"const\s*\[\s*([A-Za-z_]\w*)\s*,\s*set[A-Za-z_]\w*\s*\]\s*=\s*useState";
        foreach (Match match in Regex.Matches(tsxCode, useStatePattern))
        {
            suggestions.Add(match.Groups[1].Value);
        }

        // Interface attribute isimleri
        const string interfaceAttributePattern = @"\b([A-Za-z_]\w*)\s*\??\s*:\s*[\w\[\]]+";
        foreach (Match match in Regex.Matches(tsxCode, interfaceAttributePattern))
        {
            suggestions.Add(match.Groups[1].Value);
        }

        return new List<string>(suggestions);
    }
}