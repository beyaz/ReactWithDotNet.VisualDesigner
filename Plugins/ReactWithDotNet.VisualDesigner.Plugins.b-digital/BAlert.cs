using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[TsImport(Name = "BAlert", Package = "b-core-alert")]
sealed class BAlert : PluginComponentBase
{
    [Suggestions("success , info , warning , error")]
    [JsTypeInfo(JsType.String)]
    public string severity { get; set; }

    [Suggestions("standard , outlined , filled")]
    [JsTypeInfo(JsType.String)]
    public string variant { get; set; }

    protected override Element render()
    {
        return new div
        {
            Id(id), OnClick(onMouseClick),

            new Alert
            {
                severity = severity,
                
                variant  = variant,

                children = { children }
            }
        };
    }
}