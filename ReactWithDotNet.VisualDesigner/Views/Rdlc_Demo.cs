namespace ReactWithDotNet.VisualDesigner.Views;

static class Rdlc_Demo
{
    
    
    static LineCollection GenerateHtml(string UpLogoDataUrl, IReadOnlyList<AnyRecord> records)
    {
        #region Designer Generated Code

        return
        [
            $"""new img alt=Logo src={UpLogoDataUrl} style = "width: "177.65px"; height: "81.5547px"; objectFit: "contain"">"""
        ];

        #endregion
    }

    class LineCollection : List<string>
    {
    }
    
    class AnyRecord
    {
        public string Name { get; set; }
        
        public string Amount { get; set; }
    }
}