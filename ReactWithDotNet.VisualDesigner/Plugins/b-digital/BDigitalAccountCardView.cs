﻿using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BDigitalAccountCardView), Package = "b-digital-account-card-view")]
sealed class BDigitalAccountCardView : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string accounts { get; set; }
            
    [JsTypeInfo(JsType.Array)]
    public string cards { get; set; }
            
    [JsTypeInfo(JsType.Number)]
    public string selectedIndex { get; set; }
            
    [JsTypeInfo(JsType.Number)]
    public string isCardSelected { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onSelectedIndexChange { get; set; }
            
    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
    {
        if (node.Tag == nameof(BDigitalAccountCardView))
        {
            var selectedIndexProp = node.Properties.FirstOrDefault(x => x.Name == nameof(selectedIndex));
            var isCardSelectedProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isCardSelected));
            var onSelectedIndexChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onSelectedIndexChange));

            if (selectedIndexProp is not null && isCardSelectedProp is not null)
            {
                var properties = node.Properties;

                TsLineCollection lines =
                [
                    "(selectedAccountCardIndex: number, isCardSelected: boolean) =>",
                    "{",
                         
                    $"       {selectedIndexProp.Value} = selectedAccountCardIndex;",
                    $"       {isCardSelectedProp.Value} = isCardSelected;"
                ];

                lines.Add(new []
                {
                    Plugin.GetUpdateStateLine(selectedIndexProp.Value),
                    Plugin.GetUpdateStateLine(isCardSelectedProp.Value)
                }.Distinct());
                        

                if (onSelectedIndexChangeProp is not null)
                {
                    if (IsAlphaNumeric(onSelectedIndexChangeProp.Value))
                    {
                        lines.Add(onSelectedIndexChangeProp.Value + "(selectedAccountCardIndex, isCardSelected);");
                    }
                    else
                    {
                        lines.Add(onSelectedIndexChangeProp.Value);
                    }
                }

                lines.Add("}");

                if (onSelectedIndexChangeProp is not null)
                {
                    onSelectedIndexChangeProp = onSelectedIndexChangeProp with
                    {
                        Value = lines.ToTsCode()
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == onSelectedIndexChangeProp.Name), onSelectedIndexChangeProp);
                }
                else
                {
                    properties = properties.Add(new()
                    {
                        Name  = nameof(onSelectedIndexChange),
                        Value = lines.ToTsCode()
                    });
                }

                node = node with { Properties = properties };
            }
        }

        return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
    }

    protected override Element render()
    {
        var textContent = string.Empty;
        if (cards.HasValue())
        {
            textContent = cards;
        }

        if (accounts.HasValue())
        {
            textContent += " | " + accounts;
        }

        return new div
        {
            Id(id), OnClick(onMouseClick),
                    
            new FlexRow(AlignItemsCenter, Padding(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(83), JustifyContentSpaceBetween)
            {
                PositionRelative,
                new div(Color("rgb(0 0 0 / 60%)"))
                {
                    "Hesap / Kart Seçimi",
                            
                    PositionAbsolute,
                    Top(-16),
                    Left(8),
                    Transform("scale(0.942723)"),
                            
                    WhiteSpaceNoWrap,
                            
                    Background(White),
                    PaddingX(8)
                },
                        
                        
                new FlexColumn
                {
                    new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400)
                    {
                        textContent
                    },
                            
                    new div(Color(rgba(0, 0, 0, 0.87)), FontSize16, FontWeight400)
                    {
                        "0000000-1"
                    }
                            
                },

                new FlexRow(AlignItemsCenter, TextAlignRight, Gap(8))
                {
                    new FlexColumn
                    {
                        new div(FontWeight700) { "73.148,00 TL" },
                        new div(FontWeight400,FontSize16, Color(rgba(0, 0, 0, 0.6))) { "Cari Hesap" }
                    },

                    new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Color(rgb(117, 117, 117)))
                    {
                        new path
                        {
                            d    = "M7 10l5 5 5-5z",
                            fill = "#757575"
                        }
                    }
                }
            }
        };
    }
}