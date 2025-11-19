namespace ReactWithDotNet.VisualDesigner.TsxAnalyze;

public class TsxAnalysisResult
{
    public List<string> Functions { get; set; } = new();
    public List<StateInfo> States { get; set; } = new();
}

public class StateInfo
{
    public string State { get; set; }
    public string Setter { get; set; }
    public string Type { get; set; } // number, string, boolean, dictionary, unknown
}