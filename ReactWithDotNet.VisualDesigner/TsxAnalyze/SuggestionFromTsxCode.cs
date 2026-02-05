using System.IO;
using System.Text.RegularExpressions;

namespace ReactWithDotNet.VisualDesigner.TsxAnalyze;

static class SuggestionFromTsxCode
{
    public static async Task<Result<IReadOnlyList<string>>> GetAllVariableSuggestionsInFile(string tsxFilePath)
    {
        var list = new List<string>();

        if (tsxFilePath is null || !File.Exists(tsxFilePath))
        {
            return list;
        }

        var fileContent = await File.ReadAllTextAsync(tsxFilePath);

        foreach (var line in fileContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // is relative path in project
            if (line.Contains('"' + "../"))
            {
                const string pathPattern = """
                                           "([^"]+)"
                                           """;

                var match = Regex.Match(line, pathPattern);
                if (match.Success)
                {
                    var relativeFolderPath = match.Groups[1].Value;

                    relativeFolderPath = Path.Combine(Path.GetDirectoryName(tsxFilePath)!, relativeFolderPath);

                    relativeFolderPath = Path.GetFullPath(relativeFolderPath);

                    if (File.Exists(relativeFolderPath + ".ts"))
                    {
                        var suggestionsFromRelativeFile = await GetAllVariableSuggestionsInFile(relativeFolderPath + ".ts");
                        if (suggestionsFromRelativeFile.HasError)
                        {
                            return suggestionsFromRelativeFile.Error;
                        }

                        list.Add(suggestionsFromRelativeFile.Value);
                    }
                    else if (Directory.Exists(relativeFolderPath))
                    {
                        foreach (var file in Directory.GetFiles(relativeFolderPath, "*.ts", SearchOption.TopDirectoryOnly))
                        {
                            var suggestionsFromRelativeFile = await GetAllVariableSuggestionsInFile(file);
                            if (suggestionsFromRelativeFile.HasError)
                            {
                                return suggestionsFromRelativeFile.Error;
                            }

                            list.Add(suggestionsFromRelativeFile.Value);
                        }
                    }
                }
            }
        }

        list.AddRange(CalculateVariableSuggestions(fileContent));

        return list.Where(x => x.Length > 1).Distinct().ToList();
    }

    static IReadOnlyList<string> CalculateVariableSuggestions(string tsxCode)
    {
        var suggestions = new HashSet<string>();

        var patterns = new[]
        {
            // f u n c t i o n s
            new { Regex = @"function\s+([A-Za-z_]\w*)", GroupIndex = 1 },

            new { Regex = @"const\s+([A-Za-z_]\w*)\s*=\s*\(", GroupIndex = 1 },

            // v a r i a b l e s
            new { Regex = @"(?:let|const|var)\s+([A-Za-z_]\w*)", GroupIndex = 1 },

            // u s e s t a t e
            new { Regex = @"const\s*\[\s*([A-Za-z_]\w*)\s*,\s*set[A-Za-z_]\w*\s*\]\s*=\s*useState", GroupIndex = 1 },

            new { Regex = @"const\s*\[\s*([A-Za-z_]\w*)\s*,\s*set[A-Za-z_]\w*\s*\]\s*=\s*React.useState", GroupIndex = 1 },

            // a t t r i b u t e s
            new { Regex = @"\b([A-Za-z_]\w*)\s*\??\s*:\s*[\w\[\]]+", GroupIndex = 1 }
        };

        foreach (var pattern in patterns)
        {
            foreach (Match match in Regex.Matches(tsxCode, pattern.Regex))
            {
                if (match.Groups.Count > pattern.GroupIndex && match.Groups[pattern.GroupIndex].Success)
                {
                    suggestions.Add(match.Groups[pattern.GroupIndex].Value);
                }
            }
        }

        return new List<string>(suggestions);
    }
}