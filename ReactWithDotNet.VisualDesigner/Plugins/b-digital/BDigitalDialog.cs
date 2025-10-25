﻿namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BDigitalDialog", Package = "b-digital-dialog")]
sealed class BDigitalDialog : PluginComponentBase
{
    [JsTypeInfo(JsType.Boolean)]
    public string displayCloseIcon { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string displayOkButton { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string fullScreen { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string open { get; set; }

    [JsTypeInfo(JsType.String)]
    public string title { get; set; }

    [Suggestions("error , warning , info , success")]
    [JsTypeInfo(JsType.String)]
    public string type { get; set; }

    protected override Element render()
    {
        return new div(Background(rgba(0, 0, 0, 0.5)), Padding(24), BorderRadius(8))
        {
            Id(id), OnClick(onMouseClick),
            new div(Background("white"), BorderRadius(8), Padding(16))
            {
                // TOP BAR
                new FlexRow(JustifyContentSpaceBetween, AlignItemsCenter, PaddingY(16))
                {
                    new div(FontSize20, FontWeight400, LineHeight("160%"), LetterSpacing("0.15px")) { title },

                    displayCloseIcon == "false" || displayOkButton == "false" ? null :
                        new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24))
                        {
                            new path
                            {
                                d = "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"
                            }
                        }
                },

                SpaceY(12),

                children
            }
        };
    }
}