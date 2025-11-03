using System.Reflection;
using System.Text;
using ReactWithDotNet.ThirdPartyLibraries.MonacoEditorReact;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Views;

class ProjectSelectionView : Component<ProjectSelectionView.State>
{
    protected override Element render()
    {
        return new div(DisplayFlex, AlignItemsCenter, JustifyContentCenter, PositionRelative, Border(1, solid, "#d5d5d8"), BorderRadius(4), Height(36), WidthFitContent)
        {
            
            
        };
    }

    internal class State
    {
        
    }
}