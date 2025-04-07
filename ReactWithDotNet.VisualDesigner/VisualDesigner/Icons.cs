namespace ReactWithDotNet.VisualDesigner;

sealed class IconClose : PureComponent
{
    protected override Element render()
    {
        return new svg(Fill("currentColor"), ViewBox(0, 0, 18, 18))
        {
            new path
            {
                d = "M8.44 9.5L6 7.06A.75.75 0 1 1 7.06 6L9.5 8.44 11.94 6A.75.75 0 0 1 13 7.06L10.56 9.5 13 11.94A.75.75 0 0 1 11.94 13L9.5 10.56 7.06 13A.75.75 0 0 1 6 11.94L8.44 9.5z"
            }
        };
    }
}

sealed class IconChecked : PureComponent
{
    protected override Element render()
    {
        return new svg(Fill("currentColor"), ViewBox(0, 0, 14, 14))
        {
            new path
            {
                d    = "M4.86199 11.5948C4.78717 11.5923 4.71366 11.5745 4.64596 11.5426C4.57826 11.5107 4.51779 11.4652 4.46827 11.4091L0.753985 7.69483C0.683167 7.64891 0.623706 7.58751 0.580092 7.51525C0.536478 7.44299 0.509851 7.36177 0.502221 7.27771C0.49459 7.19366 0.506156 7.10897 0.536046 7.03004C0.565935 6.95111 0.613367 6.88 0.674759 6.82208C0.736151 6.76416 0.8099 6.72095 0.890436 6.69571C0.970973 6.67046 1.05619 6.66385 1.13966 6.67635C1.22313 6.68886 1.30266 6.72017 1.37226 6.76792C1.44186 6.81567 1.4997 6.8786 1.54141 6.95197L4.86199 10.2503L12.6397 2.49483C12.7444 2.42694 12.8689 2.39617 12.9932 2.40745C13.1174 2.41873 13.2343 2.47141 13.3251 2.55705C13.4159 2.64268 13.4753 2.75632 13.4938 2.87973C13.5123 3.00315 13.4888 3.1292 13.4271 3.23768L5.2557 11.4091C5.20618 11.4652 5.14571 11.5107 5.07801 11.5426C5.01031 11.5745 4.9368 11.5923 4.86199 11.5948Z",
                fill = "currentColor"
            }
        };
    }
}

sealed class IconMinus : PureComponent
{
    protected override Element render()
    {
        return new svg(ViewBox(0, 0, 16, 16), svg.Size(16), Fill("currentColor"))
        {
            new path { fill = "currentColor", d = "M12 8.667H4A.669.669 0 0 1 3.333 8c0-.367.3-.667.667-.667h8c.367 0 .667.3.667.667 0 .367-.3.667-.667.667Z" }
        };
    }
}

sealed class IconPlus : PureComponent
{
    protected override Element render()
    {
        return new svg(ViewBox(0, 0, 16, 16), svg.Size(16), Fill("currentColor"))
        {
            new path { fill = "currentColor", d = "M12 8.667H8.667V12c0 .367-.3.667-.667.667A.669.669 0 0 1 7.333 12V8.667H4A.669.669 0 0 1 3.333 8c0-.367.3-.667.667-.667h3.333V4c0-.366.3-.667.667-.667.367 0 .667.3.667.667v3.333H12c.367 0 .667.3.667.667 0 .367-.3.667-.667.667Z" }
        };
    }
}

sealed class IconLayers : PureComponent
{
    protected override Element render()
    {
        return new svg( svg.FocusableFalse, Fill("currentColor"), ViewBox(0, 0, 16, 16))
        {
            new path
            {
                fillRule = "evenodd",
                clipRule = "evenodd",
                d        = "M7.65399 0.0817342C7.87195 -0.0272447 8.1285 -0.0272447 8.34646 0.0817342L15.5723 3.69466C15.8346 3.8258 16.0003 4.09387 16.0003 4.38712C16.0003 4.68036 15.8346 4.94844 15.5723 5.07958L8.34646 8.6925C8.1285 8.80148 7.87195 8.80148 7.65399 8.6925L0.428149 5.07958C0.165863 4.94844 0.000183105 4.68036 0.000183105 4.38712C0.000183105 4.09387 0.165863 3.8258 0.428149 3.69466L7.65399 0.0817342ZM2.50554 4.38712L8.00022 7.13446L13.4949 4.38712L8.00022 1.63978L2.50554 4.38712Z"
            },
            new path
            {
                fillRule = "evenodd",
                clipRule = "evenodd",
                d        = "M0.0819038 7.65372C0.273122 7.27128 0.738162 7.11627 1.1206 7.30749L8.00021 10.7473L14.8798 7.30749C15.2623 7.11627 15.7273 7.27128 15.9185 7.65372C16.1097 8.03616 15.9547 8.5012 15.5723 8.69242L8.34644 12.3053C8.12848 12.4143 7.87194 12.4143 7.65398 12.3053L0.428135 8.69242C0.0456986 8.5012 -0.109315 8.03616 0.0819038 7.65372Z"
            },
            new path
            {
                fillRule = "evenodd",
                clipRule = "evenodd",
                d        = "M0.0819038 11.2666C0.273122 10.8842 0.738162 10.7292 1.1206 10.9204L8.00021 14.3602L14.8798 10.9204C15.2623 10.7292 15.7273 10.8842 15.9185 11.2666C16.1097 11.6491 15.9547 12.1141 15.5723 12.3053L8.34644 15.9183C8.12848 16.0272 7.87194 16.0272 7.65398 15.9183L0.428135 12.3053C0.0456986 12.1141 -0.109315 11.6491 0.0819038 11.2666Z"
            }
        };
    }
}


sealed class IconSettings : PureComponent
{
    protected override Element render()
    {
        return new svg(Fill(none), ViewBox(0, 0, 24, 24), Size("1em"))
        {
            new path
            {
                d              = "M5.621 14.963l1.101.172c.813.127 1.393.872 1.333 1.71l-.081 1.137a.811.811 0 00.445.787l.814.4c.292.145.641.09.88-.134l.818-.773a1.55 1.55 0 012.138 0l.818.773a.776.776 0 00.88.135l.815-.402a.808.808 0 00.443-.785l-.08-1.138c-.06-.838.52-1.583 1.332-1.71l1.101-.172a.798.798 0 00.651-.62l.201-.9a.816.816 0 00-.324-.847l-.918-.643a1.634 1.634 0 01-.476-2.132l.555-.988a.824.824 0 00-.068-.907l-.563-.723a.78.78 0 00-.85-.269l-1.064.334a1.567 1.567 0 01-1.928-.949l-.407-1.058a.791.791 0 00-.737-.511l-.903.002a.791.791 0 00-.734.516l-.398 1.045a1.566 1.566 0 01-1.93.956l-1.11-.348a.78.78 0 00-.851.27l-.56.724a.823.823 0 00-.062.91l.568.99c.418.73.213 1.666-.469 2.144l-.907.636a.817.817 0 00-.324.847l.2.9c.072.325.33.57.651.62z",
                stroke         = "currentColor",
                strokeWidth    = 1.5,
                strokeLinecap  = "round",
                strokeLinejoin = "round"
            },
            new path
            {
                d              = "M13.591 10.409a2.25 2.25 0 11-3.183 3.182 2.25 2.25 0 013.183-3.182z",
                stroke         = "currentColor",
                strokeWidth    = 1.5,
                strokeLinecap  = "round",
                strokeLinejoin = "round"
            }
        };
    }
}


sealed class IconSave : PureComponent
{
    protected override Element render()
    {
        return new svg(ViewBox(0, 0, 24, 24), Fill(none), svg.Size(24), Stroke("currentColor"), StrokeWidth("2"), StrokeLinecap("round"), StrokeLinejoin("round"))
        {
            new path
            {
                d = "M17 21H7a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10l4 4v12a2 2 0 0 1-2 2z"
            },
            new path
            {
                d = "M9 21V12h6v9"
            },
            new path
            {
                d = "M9 3h6v5H9z"
            }
        };
    }
}


sealed class IconExport : PureComponent
{
    protected override Element render()
    {
        return new svg(ViewBox(0, 0, 24, 24), Fill(none), svg.Size(24), Stroke("currentColor"), StrokeWidth("2"), StrokeLinecap("round"), StrokeLinejoin("round"))
        {
            new path
            {
                d = "M5 12v6a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-6"
            },
            new polyline(polyline.Points("16 6 12 2 8 6")),
            new line
            {
                x1 = 12,
                y1 = 2,
                x2 = 12,
                y2 = 15
            }
        };
    }
}

sealed class IconFlexRow : PureComponent
{
    protected override Element render()
    {
        return new svg( Fill(none), ViewBox(0, 0, 24, 24),  Size(16))
        {
            new path
            {
                stroke         = "currentColor",
                strokeLinecap  = "round",
                strokeLinejoin = "round",
                strokeWidth    = 1.5,
                d              = "M5.75 19.25h3.5a1 1 0 001-1V5.75a1 1 0 00-1-1h-3.5a1 1 0 00-1 1v12.5a1 1 0 001 1zm9 0h3.5a1 1 0 001-1V5.75a1 1 0 00-1-1h-3.5a1 1 0 00-1 1v12.5a1 1 0 001 1z"
            }
        };
    }
}

sealed class IconFlexColumn : PureComponent
{
    protected override Element render()
    {
        return new svg( Fill(none), ViewBox(0, 0, 24, 24),  Size(16))
        {
            new path
            {
                stroke         = "currentColor",
                strokeLinecap  = "round",
                strokeLinejoin = "round",
                strokeWidth    = 1.5,
                d              = "M5.75 10.25h12.5a1 1 0 001-1v-3.5a1 1 0 00-1-1H5.75a1 1 0 00-1 1v3.5a1 1 0 001 1zm0 9h12.5a1 1 0 001-1v-3.5a1 1 0 00-1-1H5.75a1 1 0 00-1 1v3.5a1 1 0 001 1z"
            }
        };
    }
}

sealed class IconText : PureComponent
{
    protected override Element render()
    {
        return new svg( Fill(none), ViewBox(0, 0, 24, 24),  Size(16))
        {
            new path
            {
                stroke         = "currentColor",
                strokeLinecap  = "round",
                strokeLinejoin = "round",
                strokeWidth    = 1.5,
                d              = "M18.25 7.25v-1.5H5.75v1.5M12 6v12.25m0 0h-1.25m1.25 0h1.25"
            }
        };
    }
}

sealed class IconHeader : PureComponent
{
    protected override Element render()
    {
        return new svg( Fill(none), ViewBox(0, 0, 24, 24),  Size(16))
        {
            new path
            {
                stroke         = "currentColor",
                strokeLinecap  = "round",
                strokeLinejoin = "round",
                strokeWidth    = 1.5,
                d              = "M5.75 5.75h1.5m0 0h1m-1 0v6m0 6.5h-1.5m1.5 0h1m-1 0v-6.5m0 0h9.5m0 0v-6m0 6v6.5m1.5-12.5h-1.5m0 0h-1m1 12.5h1.5m-1.5 0h-1"
            }
        };
    }
}

sealed class IconLink : PureComponent
{
    protected override Element render()
    {
        return new svg( Fill(none), ViewBox(0, 0, 24, 24),  Size(16))
        {
            new path
            {
                stroke         = "currentColor",
                strokeLinecap  = "round",
                strokeLinejoin = "round",
                strokeWidth    = 1.5,
                d              = "M16.75 13.25L18 12a4.243 4.243 0 000-6v0a4.243 4.243 0 00-6 0l-1.25 1.25m-3.5 3.5L6 12a4.243 4.243 0 000 6v0a4.243 4.243 0 006 0l1.25-1.25m1-7l-4.5 4.5"
            }
        };
    }
}

sealed class IconImage : PureComponent
{
    protected override Element render()
    {
        return new svg( Fill(none), ViewBox(0, 0, 24, 24),  Size(16))
        {
            new path
            {
                stroke         = "currentColor",
                strokeLinecap  = "round",
                strokeLinejoin = "round",
                strokeWidth    = 1.5,
                d              = "M4.75 16l2.746-3.493a2 2 0 013.09-.067L13 15.25m-2.085-2.427a645.29 645.29 0 002.576-3.31 2 2 0 013.094-.073L19 12.25m-12.25 7h10.5a2 2 0 002-2V6.75a2 2 0 00-2-2H6.75a2 2 0 00-2 2v10.5a2 2 0 002 2z"
            }
        };
    }
}

sealed class IconReact : PureComponent
{
    protected override Element render()
    {
        return new svg(ViewBox(0, 0, 64, 64), Size(16))
        {
            new path
            {
                fillRule = "evenodd",
                clipRule = "evenodd",
                d        = "M32 16.42c-2.305-1.573-4.574-2.842-6.712-3.758-2.378-1.02-4.69-1.644-6.768-1.702-2.084-.058-4.121.458-5.612 1.948-1.49 1.49-2.006 3.528-1.948 5.612.058 2.077.683 4.39 1.702 6.768.916 2.138 2.185 4.407 3.759 6.712-1.574 2.305-2.843 4.574-3.76 6.712-1.018 2.377-1.643 4.69-1.701 6.768-.058 2.084.458 4.121 1.948 5.612 1.49 1.49 3.528 2.006 5.612 1.948 2.077-.058 4.39-.683 6.768-1.702 2.138-.916 4.407-2.185 6.712-3.759 2.305 1.574 4.574 2.843 6.712 3.76 2.378 1.018 4.69 1.643 6.768 1.701 2.084.058 4.121-.458 5.612-1.948 1.49-1.49 2.006-3.528 1.948-5.612-.058-2.077-.683-4.39-1.702-6.768-.916-2.138-2.185-4.407-3.759-6.712 1.574-2.305 2.843-4.574 3.76-6.712 1.018-2.378 1.643-4.69 1.701-6.768.058-2.084-.458-4.121-1.948-5.612-1.49-1.49-3.528-2.006-5.612-1.948-2.077.058-4.39.683-6.768 1.702-2.138.916-4.407 2.185-6.712 3.759zM15.736 48.264c.463.463 1.274.818 2.672.779 1.404-.04 3.203-.48 5.304-1.38 1.533-.657 3.175-1.537 4.872-2.62a62.984 62.984 0 01-5.07-4.557 62.984 62.984 0 01-4.556-5.07c-1.083 1.699-1.963 3.34-2.62 4.873-.9 2.101-1.34 3.9-1.38 5.304-.039 1.398.316 2.21.778 2.672zM21.361 32a58.18 58.18 0 004.982 5.657A58.18 58.18 0 0032 42.639a58.192 58.192 0 005.657-4.982A58.192 58.192 0 0042.639 32a58.178 58.178 0 00-4.982-5.657A58.192 58.192 0 0032 21.361a58.192 58.192 0 00-5.657 4.982A58.192 58.192 0 0021.361 32zm7.223-13.042c-1.697-1.083-3.34-1.963-4.872-2.62-2.101-.9-3.9-1.34-5.304-1.38-1.398-.039-2.21.316-2.671.778-.463.463-.818 1.274-.779 2.672.04 1.404.48 3.203 1.38 5.304.657 1.533 1.537 3.175 2.62 4.872a62.984 62.984 0 014.557-5.07 62.984 62.984 0 015.07-4.556zm6.832 0a62.95 62.95 0 015.07 4.557 62.984 62.984 0 014.556 5.07c1.083-1.698 1.963-3.34 2.62-4.873.9-2.101 1.34-3.9 1.38-5.304.039-1.398-.316-2.21-.779-2.672-.462-.462-1.273-.817-2.671-.778-1.404.04-3.203.48-5.304 1.38-1.533.657-3.174 1.537-4.872 2.62zm9.626 16.458a62.984 62.984 0 01-4.557 5.07 62.984 62.984 0 01-5.07 4.556c1.699 1.083 3.34 1.963 4.873 2.62 2.101.9 3.9 1.34 5.304 1.38 1.398.039 2.21-.316 2.672-.779.462-.462.817-1.273.778-2.671-.04-1.404-.48-3.203-1.38-5.304-.657-1.533-1.537-3.174-2.62-4.872z",
                fill     = "currentColor"
            }
        };
    }
}

sealed class IconSpaceVertical : PureComponent
{
    protected override Element render()
    {
        return new FlexRowCentered(Size(14))
        {
            new div(HeightFull, Width(1), Background(Gray300))
        };
    }
}

sealed class IconSpaceHorizontal : PureComponent
{
    protected override Element render()
    {
        return new FlexRowCentered(Size(14))
        {
            new div(WidthFull, Height(1), Background(Gray300))
        };
    }
}

sealed class IconArrowRightOrDown : PureComponent
{
    public bool IsArrowDown { get; init; } = true;

    protected override Element render()
    {
        var arrowDown = new svg(svg.ViewBox(0, 0, 24, 24), svg.Size(24))
        {
            new path { d = "M8.12 9.29 12 13.17l3.88-3.88c.39-.39 1.02-.39 1.41 0 .39.39.39 1.02 0 1.41l-4.59 4.59c-.39.39-1.02.39-1.41 0L6.7 10.7a.9959.9959 0 0 1 0-1.41c.39-.38 1.03-.39 1.42 0z" }
        };

        return arrowDown + Transition("all", 400) + Transform(IsArrowDown ? "rotate(0deg)" : "rotate(-90deg)");
    }
}