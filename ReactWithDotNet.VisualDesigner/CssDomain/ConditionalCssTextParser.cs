using System.Text.RegularExpressions;

namespace ReactWithDotNet.VisualDesigner;

static class ConditionalCssTextParser
{
    public static (bool success, string condition, string left, string right) TryParseConditionalValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (false, null, null, null);
        }

        // condition ? left : right  (right opsiyonel)
        var pattern = @"^\s*(?<condition>[^?]+?)\s*\?\s*(?<left>[^:]+?)\s*(?::\s*(?<right>.+))?$";
        var match = Regex.Match(value, pattern);

        if (match.Success)
        {
            var condition = match.Groups["condition"].Value.Trim();
            var left = match.Groups["left"].Value.Trim();
            var right = match.Groups["right"].Success ? match.Groups["right"].Value.Trim() : null;
            return (true, condition, left, right);
        }

        return (false, null, null, null);
    }
}