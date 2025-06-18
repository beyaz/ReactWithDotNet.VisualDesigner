using FluentAssertions;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
        (await NextJs_with_Tailwind.ExportAll(1)).Success.Should().BeTrue();
    }
    
    
    //public static VisualElementModel Fix(this VisualElementModel model)
    //{
    //    var bindPropertyIndex = -1;
        
    //    string bindValue = null;
        
    //    for (var i = 0; i < model.Properties.Count; i++)
    //    {
    //        var result = TryParsePropertyValue(model.Properties[i]);
    //        if (result.HasValue)
    //        {
    //            var name = result.Name;
    //            var value = result.Value;

    //            if (name == "-bind")
    //            {
    //                bindPropertyIndex = i;

    //                bindValue = value;
                    
    //                if (model.Text.HasNoValue())
    //                {
    //                    throw new ArgumentException("Text cannot be null");
    //                }
    //            }
    //        }
    //    }

    //    if (bindPropertyIndex >= 0)
    //    {
    //        model.Properties.RemoveAt(bindPropertyIndex);
            
    //        model.Properties.Insert(0,$"-text: {ClearConnectedValue(bindValue)}");
    //        model.Properties.Insert(1,$"--text: '{TryClearStringValue(model.Text)}'");

    //        model.Text = null;
    //    }
    //    else if (model.Text.HasValue())
    //    {
    //        model.Properties.Insert(0, $"-text: '{TryClearStringValue(model.Text)}'");

    //        model.Text = null;
    //    }

    //    foreach (var child in model.Children)
    //    {
    //        Fix(child);
    //    }

    //    return model;
    //}
}