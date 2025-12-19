namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

sealed class TsLineCollection: List<string>
{

    public void Add(IEnumerable<string> lines)
    {
        AddRange(lines);
    }
    
    public void Add(IReadOnlyList<string> lines)
    {
        if (lines is null)
        {
            return;
        }
        
        AddRange(lines);
    }

    public string ToTsCode()
    {
        return string.Join(Environment.NewLine, from line in this where line is not null select line);
    }
}