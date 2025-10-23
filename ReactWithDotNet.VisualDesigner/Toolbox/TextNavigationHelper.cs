namespace Toolbox;

static class TextNavigationHelper
{
   
    
    public static Maybe<int> FindLineIndexStartsWith(this List<string> lines, int startIndex, int leftSpaceLength, params string[] searchTexts)
    {
        var left = string.Empty.PadRight(leftSpaceLength, ' ');
            
        foreach (var searchText in searchTexts)
        {
            var index = lines.FindIndex(startIndex, l => l.StartsWith(left + searchText));
            if (index > 0)
            {
                return index;
            }
        }

        return None;
    }
}