namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class TransactionWizardPage : PluginComponentBase
{
    [JsTypeInfo(JsType.Boolean)]
    public string isWide { get; set; }

    protected override Element render()
    {
        return new FlexColumn(WidthFull, Padding(16), Background("#fafafa"))
        {
            children =
            {
                children
            }
        } + Id(id) + OnClick(onMouseClick);
    }
}