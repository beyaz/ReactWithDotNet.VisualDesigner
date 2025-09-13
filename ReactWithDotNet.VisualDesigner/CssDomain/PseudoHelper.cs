namespace ReactWithDotNet.VisualDesigner.CssDomain;

static class PseudoHelper
{
    public static Maybe<(string Pseudo, string NewText)> TryReadPseudo(string text)
    {
        foreach (var pseudo in MediaQueries.Keys)
        {
            var prefix = pseudo + ":";
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var newText = text.RemoveFromStart(prefix, StringComparison.OrdinalIgnoreCase);

                return (prefix.RemoveFromEnd(":").ToLower(), newText);
            }
        }

        return None;
    }
    
    public static Result<Func<StyleModifier[], StyleModifier>> GetPseudoFunction(string pseudoName)
    {
        if (MediaQueries.TryGetValue(pseudoName, out var func))
        {
            return func;
        }

        return new ArgumentOutOfRangeException($"{pseudoName} not recognized");
    }
    
    static readonly Dictionary<string, Func<StyleModifier[], StyleModifier>> MediaQueries = new(StringComparer.OrdinalIgnoreCase)
    {
        { "hover", Hover },
        { "Focus", Focus },
        { "SM", SM },
        { "MD", MD },
        { "LG", LG },
        { "XL", XL },
        { "XXL", XXL }
    };
}