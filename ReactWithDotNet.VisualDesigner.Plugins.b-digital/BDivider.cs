using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BDivider), Package = "b-divider")]
sealed class BDivider : PluginComponentBase
{
    protected override Element render()
    {
        return new Divider
        {
            id = id,

            onClick = onMouseClick,

            children = { children },
            
            style = { Margin(12) }
        };
    }
}