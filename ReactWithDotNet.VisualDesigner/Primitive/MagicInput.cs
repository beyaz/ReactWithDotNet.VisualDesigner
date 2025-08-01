﻿namespace ReactWithDotNet.VisualDesigner.Primitive;

delegate Task InputChangeHandler(string senderName, string newValue);

delegate Task InputFocusHandler(string senderName);

delegate Task InputPasteHandler(string text);

sealed class MagicInput : Component<MagicInput.State>
{
    public bool AutoFocus { get; init; }

    public bool FitContent { get; init; }
    public string Id { get; set; }

    public bool IsBold { get; init; }
    public bool IsTextAlignCenter { get; init; }
    public bool IsTextAlignRight { get; init; }

    public Func<string, Element> ItemRender { get; set; }
    public required string Name { get; init; }

    [CustomEvent]
    public InputChangeHandler OnChange { get; init; }

    [CustomEvent]
    public InputFocusHandler OnFocus { get; init; }

    public string Placeholder { get; init; }

    public IReadOnlyList<string> Suggestions { get; init; } = [];

    public string Value { get; init; }

    protected override Task constructor()
    {
        InitializeState();

        return Task.CompletedTask;
    }

    protected override Task OverrideStateFromPropsBeforeRender()
    {
        if (Value != state.InitialValue || Name != state.InitialName)
        {
            InitializeState();
        }

        return Task.CompletedTask;
    }

    protected override Element render()
    {
        return new FlexColumn(!FitContent ? WidthFull : null)
        {
            new input
            {
                type                     = "text",
                valueBind                = () => state.Value,
                valueBindDebounceTimeout = 300,
                valueBindDebounceHandler = OnTypingFinished,
                onKeyDown                = OnKeyDown,
                onClick                  = OnInputClicked,
                placeholder              = Placeholder,
                onFocus                  = OnFocused,
                onBlur                   = OnBlur,
                id                       = Id,
                autoComplete             = "off",
                spellCheck               = "false",
                style =
                {
                    OutlineNone,
                    BorderNone,
                    Appearance(none),
                    PaddingTopBottom(4),
                    Color(rgb(0, 6, 36)),
                    Height(24),
                    FitContent ? Width(CalculateTextWidth(state.Value)) : FlexGrow(1),
                    Background(transparent),
                    EditorFont(),
                    IsBold ? FontWeight600 : null,
                    IsTextAlignRight ? TextAlignRight : null,
                    IsTextAlignCenter ? TextAlignCenter : null
                },
                autoFocus = AutoFocus
            },
            ViewSuggestions
        };
    }

    Task CloseSuggestion()
    {
        state = state with { ShowSuggestions = false };

        return Task.CompletedTask;
    }

    void InitializeState()
    {
        state = new()
        {
            InitialName = Name,

            InitialValue = Value,

            Value = Value,

            IgnoreTypingFinishedEvent = state?.IgnoreTypingFinishedEvent ?? false,

            FilteredSuggestions = Suggestions ?? []
        };
    }

    Task OnBlur(FocusEvent e)
    {
        Client.GotoMethod(500, CloseSuggestion);

        return Task.CompletedTask;
    }

    [StopPropagation]
    Task OnFocused(FocusEvent e)
    {
        DispatchEvent(OnFocus, [Name]);

        return Task.CompletedTask;
    }

    [StopPropagation]
    Task OnInputClicked(MouseEvent e)
    {
        state = state with { ShowSuggestions = false };

        return Task.CompletedTask;
    }

    [KeyboardEventCallOnly("ArrowDown", "ArrowUp", "Enter", "CTRL+ArrowRight", "CTRL+S", "CTRL+s")]
    Task OnKeyDown(KeyboardEvent e)
    {
        if (!state.ShowSuggestions)
        {
            state = state with { ShowSuggestions = true };

            if (state.SelectedSuggestionOffset >= 0)
            {
                return Task.CompletedTask;
            }
        }

        var suggestions = state.FilteredSuggestions ?? [];
        if (suggestions.Count == 0)
        {
            state = state with { ShowSuggestions = false };

            if (e.key == "Enter")
            {
                if (state.Value?.Length > 0 || (state.InitialValue.HasValue() && state.Value.HasNoValue()))
                {
                    DispatchEvent(OnChange, [Name, state.Value]);
                }
            }

            return Task.CompletedTask;
        }

        if (e.key == "ArrowDown")
        {
            if (state.SelectedSuggestionOffset.HasValue)
            {
                state = state with
                {
                    SelectedSuggestionOffset = state.SelectedSuggestionOffset.Value + 1
                };
            }
            else
            {
                state = state with
                {
                    SelectedSuggestionOffset = 0
                };
            }

            if (state.SelectedSuggestionOffset >= suggestions.Count)
            {
                state = state with { SelectedSuggestionOffset = suggestions.Count - 1 };
            }
        }

        if (e.key == "ArrowUp")
        {
            if (state.SelectedSuggestionOffset.HasValue)
            {
                state = state with
                {
                    SelectedSuggestionOffset = state.SelectedSuggestionOffset.Value - 1
                };
            }
            else
            {
                state = state with
                {
                    SelectedSuggestionOffset = 0
                };
            }

            if (state.SelectedSuggestionOffset < 0)
            {
                state = state with
                {
                    SelectedSuggestionOffset = 0
                };
            }
        }

        if (e.key == "Enter" || (e.ctrlKey && e.key is "S" or "s"))
        {
            state = state with { ShowSuggestions = false };

            state = state with { IgnoreTypingFinishedEvent = true };

            if (state.SelectedSuggestionOffset is null)
            {
                if (state.Value.HasValue() && state.Value.Trim() != Value?.Trim())
                {
                    DispatchEvent(OnChange, [Name, state.Value]);
                }

                return Task.CompletedTask;
            }

            if (suggestions.Count > state.SelectedSuggestionOffset.Value)
            {
                state = state with { Value = suggestions[state.SelectedSuggestionOffset.Value] };

                DispatchEvent(OnChange, [Name, state.Value]);
            }
        }

        if (e.key == "ArrowRight")
        {
            if (state.SelectedSuggestionOffset is not null)
            {
                if (suggestions.Count > state.SelectedSuggestionOffset.Value)
                {
                    state = state with
                    {
                        Value = suggestions[state.SelectedSuggestionOffset.Value],
                        ShowSuggestions = false
                    };
                }
            }
        }

        return Task.CompletedTask;
    }

    [StopPropagation]
    Task OnSuggestionItemClicked(MouseEvent e)
    {
        state = state with
        {
            ShowSuggestions = false,
            SelectedSuggestionOffset = int.Parse(e.target.data["INDEX"]),
            Value = state.FilteredSuggestions[state.SelectedSuggestionOffset!.Value]
        };

        DispatchEvent(OnChange, [Name, state.Value]);

        return Task.CompletedTask;
    }

    Task OnTypingFinished()
    {
        if (state.IgnoreTypingFinishedEvent)
        {
            state = state with { IgnoreTypingFinishedEvent = false };
            return Task.CompletedTask;
        }

        state = state with
        {
            ShowSuggestions = true,
            SelectedSuggestionOffset = null,
            FilteredSuggestions = Suggestions.Where(x => x.Replace(" ", string.Empty).Contains((state.Value + string.Empty).Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase))
                .Take(5).ToList()
        };

        return Task.CompletedTask;
    }

    Element ViewSuggestions()
    {
        if (!state.ShowSuggestions)
        {
            return null;
        }

        var suggestions = state.FilteredSuggestions ?? [];

        if (suggestions.Count == 0)
        {
            return null;
        }

        return new FlexColumn(PositionRelative, SizeFull)
        {
            Zindex3,
            new FlexColumn(PositionAbsolute, MinWidth(200), Top(4), HeightAuto, Background(White), BoxShadow(0, 6, 6, 0, rgba(22, 45, 61, .06)), Padding(5), BorderRadius(5))
            {
                Zindex4,
                IsTextAlignRight ? Right(0) : null,
                IsTextAlignCenter ? Right(none) : null,

                suggestions.Take(5).Select(ToOption)
            },

            IsTextAlignCenter ? AlignItemsCenter : null
        };

        Element ToOption(string text, int index)
        {
            return new div(BorderRadius(4), OnClick(OnSuggestionItemClicked))
            {
                Data("INDEX", index),

                ItemRender is not null ? ItemRender(text) : text,
                PaddingLeft(5),
                Color(rgb(0, 6, 36)),
                WhiteSpaceNormal, OverflowWrapAnywhere,

                CursorDefault,

                Hover(Background(Gray100)),

                index == state.SelectedSuggestionOffset ? Color("#495cef") + Background("#e7eaff") : null
            };
        }
    }

    internal record State
    {
        public IReadOnlyList<string> FilteredSuggestions { get; init; }

        public bool IgnoreTypingFinishedEvent { get; init; }

        public required string InitialName { get; init; }

        public string InitialValue { get; init; }

        public int? SelectedSuggestionOffset { get; init; }

        public bool ShowSuggestions { get; init; }

        public string Value { get; init; }
    }
}