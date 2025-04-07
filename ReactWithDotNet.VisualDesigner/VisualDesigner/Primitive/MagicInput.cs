namespace ReactWithDotNet.VisualDesigner.Primitive;

delegate Task InputChangeHandler( string senderName, string newValue);
delegate Task InputFocusHandler( string senderName);

sealed class MagicInput : Component<MagicInput.State>
{
    public required string Name { get; init; }
    
    [CustomEvent]
    public InputChangeHandler OnChange { get; init; }
    
    [CustomEvent]
    public InputFocusHandler OnFocus { get; init; }

    public IReadOnlyList<string> Suggestions { get; init; } = [];

    public string Value { get; init; }

    public bool FitContent { get; init; }

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

    public bool IsBold { get; init; }
    public bool IsTextAlignRight { get; init; }
    public string Placeholder { get; init; }
    public bool IsTextAlignCenter { get; init; }

    public bool AutoFocus { get; init; }
    public string Id { get; set; }

    protected override Element render()
    {
        return new FlexColumn(FitContent is false ? FlexGrow(1) : null)
        {
            new input
            {
                type                     = "text",
                valueBind                = () => state.Value,
                valueBindDebounceTimeout = 300,
                valueBindDebounceHandler = OnTypingFinished,
                onKeyDown                = OnKeyDown,
                onClick                  = OnInputClicked,
                placeholder = Placeholder,
                onFocus = OnFocused,
                onBlur = OnBlur,
                id=Id,
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

    Task OnBlur(FocusEvent e)
    {
        Client.GotoMethod(500, CloseSuggestion);

        return Task.CompletedTask;
    }
    
    Task CloseSuggestion()
    {
        state.ShowSuggestions = false;

        return Task.CompletedTask;
    }

    [StopPropagation]
    Task OnFocused(FocusEvent e)
    {
        DispatchEvent(OnFocus, [Name]);

        return Task.CompletedTask;
    }

    void InitializeState()
    {
        state = new()
        {
            InitialName = Name,
            
            InitialValue = Value,

            Value = Value,

            FilteredSuggestions = Suggestions ?? []
        };
    }

    [StopPropagation]
    Task OnInputClicked(MouseEvent e)
    {
        state.ShowSuggestions = !state.ShowSuggestions;

        return Task.CompletedTask;
    }

    [KeyboardEventCallOnly("ArrowDown", "ArrowUp", "Enter")]
    Task OnKeyDown(KeyboardEvent e)
    {
        if (state.ShowSuggestions is false)
        {
            state.ShowSuggestions = true;

            if (state.SelectedSuggestionOffset >= 0)
            {
                return Task.CompletedTask;
            }
        }

        var suggestions = state.FilteredSuggestions ?? [];
        if (suggestions.Count == 0)
        {
            state.ShowSuggestions = false;

            if (e.key == "Enter")
            {
                if (state.Value?.Length > 0 || (state.InitialValue.HasValue() && state.Value.HasNoValue()))
                {
                    DispatchEvent(OnChange, [Name, state.Value]);

                    return Task.CompletedTask;
                }

                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        if (e.key == "ArrowDown")
        {
            state.SelectedSuggestionOffset ??= -1;

            state.SelectedSuggestionOffset++;

            if (state.SelectedSuggestionOffset >= suggestions.Count)
            {
                state.SelectedSuggestionOffset = suggestions.Count - 1;
            }
        }

        if (e.key == "ArrowUp")
        {
            state.SelectedSuggestionOffset ??= 1;

            state.SelectedSuggestionOffset--;

            if (state.SelectedSuggestionOffset < 0)
            {
                state.SelectedSuggestionOffset = 0;
            }
        }

        if (e.key == "Enter")
        {
            state.ShowSuggestions = false;

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
                state.Value = suggestions[state.SelectedSuggestionOffset.Value];

                DispatchEvent(OnChange, [Name,state.Value]);
            }
        }

        return Task.CompletedTask;
    }

    [StopPropagation]
    Task OnSuggestionItemClicked(MouseEvent e)
    {
        state.ShowSuggestions = false;

        state.SelectedSuggestionOffset = int.Parse(e.target.data["INDEX"]);

        state.Value = state.FilteredSuggestions[state.SelectedSuggestionOffset.Value];

        DispatchEvent(OnChange, [Name, state.Value]);

        return Task.CompletedTask;
    }

    Task OnTypingFinished()
    {
        state.ShowSuggestions = true;

        state.SelectedSuggestionOffset = null;

        state.FilteredSuggestions = Suggestions.Where(x => x.Replace(" ",string.Empty).Contains((state.Value + string.Empty).Replace(" ",string.Empty), StringComparison.OrdinalIgnoreCase))
            .Take(5).ToList();

        return Task.CompletedTask;
    }

    Element ViewSuggestions()
    {
        if (state.ShowSuggestions is false)
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
            new FlexColumn(PositionAbsolute, Top(4),  HeightAuto, Background(White), BoxShadow(0, 6, 6, 0, rgba(22, 45, 61, .06)), Padding(5), BorderRadius(5))
            {
                Zindex4,
                IsTextAlignRight ? Right(0) : null,
                IsTextAlignCenter ? Right(none) : null,
                
                suggestions.Take(5).Select(ToOption)
            },
            
            IsTextAlignCenter ? AlignItemsCenter : null
        };

        Element ToOption(string code, int index)
        {
            return new div(BorderRadius(4), OnClick(OnSuggestionItemClicked))
            {
                Data("INDEX", index),

                code,
                PaddingLeft(5),
                Color(rgb(0, 6, 36)),
                WhiteSpaceNoWrap,
                CursorDefault,

                Hover(Background(Gray100)),

                index == state.SelectedSuggestionOffset ? Color("#495cef") + Background("#e7eaff") : null
            };
        }
    }

    internal class State
    {
        public IReadOnlyList<string> FilteredSuggestions { get; set; }

        public string InitialValue { get; init; }
        
        public required string InitialName { get; init; }

        public int? SelectedSuggestionOffset { get; set; }

        public bool ShowSuggestions { get; set; }

        public string Value { get; set; }
    }
}