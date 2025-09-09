namespace ReactWithDotNet.VisualDesigner.Toolbox;

static class TextNavigationHelper
{
    public static Maybe<(int index, int leftPaddingCount)> FindLineIndexStartsWith(this List<string> lines, int startIndex, params string[] searchTexts)
    {
        for (var i = 1; i < 100; i++)
        {
            foreach (var searchText in searchTexts)
            {
                var index = lines.FindIndex(startIndex, l => l.StartsWith(string.Empty.PadRight(i, ' ') + searchText));
                if (index > 0)
                {
                    return (index, i);
                }
            }
        }

        return None;
    }
}