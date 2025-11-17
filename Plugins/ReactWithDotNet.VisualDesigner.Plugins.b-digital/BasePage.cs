namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BasePage), Package = "b-digital-framework")]
sealed class BasePage : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string pageTitle { get; set; }
    
    [JsTypeInfo(JsType.Function)]
    public string handleBackClick { get; set; }

    protected override Element render()
    {
        return new FlexColumn(FontFamily("Roboto"), WidthFull, Padding(16), Background("#fafafa"))
        {
            children =
            {
                new h6(FontWeight500, FontSize20, PaddingTop(32), PaddingBottom(56))
                {
                    pageTitle
                },
                children
            }
        } + Id(id) + OnClick(onMouseClick);
    }
}