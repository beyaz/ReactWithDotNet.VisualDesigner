using System.IO;
using System.Text.RegularExpressions;

namespace ReactWithDotNet.VisualDesigner.TsxAnalyze;

static class SuggestionFromTsxCode
{
    public static async Task<Result<IReadOnlyList<string>>> GetAllVariableSuggestionsInFile(string tsxFilePath)
    {
        var list = new List<string>();

        var (success, directoryInfo) = TryNavigatePackagesDotJsonFolder(tsxFilePath);
        if (success)
        {
            foreach (var fileInfo in from f in directoryInfo.GetFiles("*", SearchOption.AllDirectories) where f.Extension is ".ts" or ".tsx" select f)
            {
                var fileContent = await File.ReadAllTextAsync(fileInfo.FullName);

                list.AddRange(CalculateVariableSuggestions(fileContent));
            }
        }

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

    static (bool success, DirectoryInfo directoryInfo) TryNavigatePackagesDotJsonFolder(string tsxFilePath)
    {
        var directoryPath = Path.GetDirectoryName(tsxFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return (false, null);
        }

        var directoryInfo = new DirectoryInfo(directoryPath);

        while (directoryInfo.Exists)
        {
            var parent = directoryInfo.Parent;
            if (parent is null || parent.Exists is false)
            {
                return (false, null);
            }

            var packages_json = Path.Combine(parent.FullName, "package.json");
            if (File.Exists(packages_json))
            {
                return (true, directoryInfo);
            }

            directoryInfo = parent;
        }

        return (false, null);
    }
}