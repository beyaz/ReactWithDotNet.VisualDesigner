namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BDigitalEmptyPage), Package = "b-digital-empty-page")]
sealed partial class BDigitalEmptyPage : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string description { get; set; }

    [Suggestions("NoAccount , WorkTime , NothingFoundInSearch , NoPackage , NoCurrentAccount , CuriositySearch , Dashboard")]
    [JsTypeInfo(JsType.String)]
    public string infoImageSource { get; set; }

    protected override Element render()
    {
        return new div
        {
            DisplayFlex, JustifyItems("center"), AlignItemsCenter, FlexDirectionColumn, Height("100%"),

            Id(id), OnClick(onMouseClick),

            new img
            {
                loading = "lazy",
                src     = FirstOrDefaultOf(from x in Images where x.Name == infoImageSource select x.Src) ?? Images[0].Src
            }
        };
    }
}