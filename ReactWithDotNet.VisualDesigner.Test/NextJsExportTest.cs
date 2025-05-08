using System.Reflection;
using ReactWithDotNet.VisualDesigner.Exporters;
using static ReactWithDotNet.VisualDesigner.Exporters.NextJs_with_Tailwind;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
         await NextJs_with_Tailwind.ExportAll(1);
        
        var components = await GetAllComponentsInProject(1);

        foreach (var component in components)
        {
            if (component.Name == "HggImage")
            {
                continue;
            }
            
            var root = component.RootElementAsJson.AsVisualElementModel();

            visitProperties(root);
        }



        static void visitProperties( VisualElementModel model )
        {
            var elementType = typeof(svg).Assembly.GetType(nameof(ReactWithDotNet) + "." + model.Tag, false);
            
            for (var i = 0; i < model.Properties.Count; i++)
            {
                var result = TryParsePropertyValue(model.Properties[i]);
                if (result.HasValue)
                {
                    var name = result.Name;
                    var value = result.Value;

                    if (IsStringValue(value) || IsConnectedValue(value))
                    {
                        continue;
                    }

                    if (elementType is not null)
                    {
                        if (elementType.GetProperty(name)?.PropertyType == typeof(double) ||
                            elementType.GetProperty(name)?.PropertyType == typeof(UnionProp<string, double>))
                        {
                            if (double.TryParse(value, out var _))
                            {
                                continue;
                            }
                        }
                        
                        if (elementType.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.PropertyType == typeof(string))
                        {
                            if (value.Contains(' ') || value.StartsWith('#'))
                            {
                                continue;
                            }
                        }
                    }


                    if (name == "w" || name == "h"|| name == "width"|| name == "height"||name == "size"
                        ||name == "-items-source-design-time-count"
                        )
                    {
                        continue;
                    }
                    
                    if (
                        name == "xmlns" || name == "fill" || name == "isAvatar" || 
                        name == "className" ||name == "class" || name == "href"|| name == "src"|| name == "alt")
                    {
                        model.Properties[i] = $"{name}='{value}'";
                        continue;
                    }
                    
                    if (name == "type" && value == "text")
                    {
                        continue;
                    }
                    if (name == "stroke-linecap" && value == "square")
                    {
                        continue;
                    }

                    value.ToString();

                }
            }

            foreach (var child in model.Children)
            {
                visitProperties(child);
            }
        }
    }
}