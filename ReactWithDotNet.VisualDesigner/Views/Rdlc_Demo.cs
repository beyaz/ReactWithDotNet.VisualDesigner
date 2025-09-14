namespace ReactWithDotNet.VisualDesigner.Views;

static class Rdlc_Demo
{
    
    
    static LineCollection GenerateHtml(string UpLogoDataUrl, IReadOnlyList<AnyRecord> records)
    {
        #region Designer Generated Code

        return new LineCollection
            {
            $"<img alt=Logo src={UpLogoDataUrl} style=\"width: 177.65px; height: 81.5547px; object-fit: contain\">",
            
            "<div style=\"display: flex; flex-direction: column; flex: 1; min-width: 300px\">",
            
            
            
            "    <div style=\"display: flex; align-items: center; margin-bottom: 10px\">",
            
            from item in records select new LineCollection
            {
            
                "        <span style=\"font-size: 10.6667px; font-weight: 800; margin-left: auto\">",
                $"            {item.Name}",
                "        </span>"
            
            }
            
            ,
            
            "    </div>",
            
         
            
            
            "</div>"
            
        };

        #endregion
    }

    class LineCollection : List<string>
    {
        public void Add(IEnumerable<LineCollection> lineCollections)
        {
            foreach (var collection in lineCollections)
            {
                AddRange(collection);
            }
        }
    }
    
    class AnyRecord
    {
        public string Name { get; set; }
        
        public string Amount { get; set; }
    }
}