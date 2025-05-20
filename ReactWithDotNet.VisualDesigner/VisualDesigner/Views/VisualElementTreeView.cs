namespace ReactWithDotNet.VisualDesigner.Views;

delegate Task OnTreeItemHover(string treeItemPath);

enum DragPosition
{
    Before,
    After,
    Inside
}

delegate Task OnTreeItemMove(string treeItemPathSourge, string treeItemPathTarget, DragPosition position);

delegate Task OnTreeItemCopyPaste(string treeItemPathSourge, string treeItemPathTarget);

delegate Task OnTreeItemDelete();

sealed class VisualElementTreeView : Component<VisualElementTreeView.State>
{
    [CustomEvent]
    public OnTreeItemCopyPaste CopyPaste { get; init; }

    public VisualElementModel Model { get; init; }

    [CustomEvent]
    public Func<Task> MouseLeave { get; init; }

    [CustomEvent]
    public OnTreeItemDelete OnDelete { get; init; }

    [CustomEvent]
    public Func<Task> OnHideInDesignerToggle { get; init; }

    public string SelectedPath { get; init; }

    [CustomEvent]
    public Func<string, Task> SelectionChanged { get; init; }

    [CustomEvent]
    public OnTreeItemHover TreeItemHover { get; init; }

    [CustomEvent]
    public OnTreeItemMove TreeItemMove { get; init; }

    protected override Element render()
    {
        if (Model is null)
        {
            return new FlexRowCentered(SizeFull) { "Empty" };
        }

        return new div(CursorDefault, Padding(5), OnMouseLeave(OnMouseLeaveHandler), OnKeyDown(On_Key_Down), TabIndex(0), OutlineNone)
        {
            ToVisual(Model, 0, "0"),
            WidthFull, HeightFull
        };
    }

    [KeyboardEventCallOnly("CTRL+c", "CTRL+v", "Delete")]
    Task On_Key_Down(KeyboardEvent e)
    {
        if (e.key == "Delete")
        {
            DispatchEvent(OnDelete, []);
            return Task.CompletedTask;
        }

        if (e.key == "c")
        {
            state.CopiedTreeItemPath = SelectedPath;
            return Task.CompletedTask;
        }

        if (e.key == "v" && state.CopiedTreeItemPath.HasValue())
        {
            DispatchEvent(CopyPaste, [state.CopiedTreeItemPath, SelectedPath]);

            state.CopiedTreeItemPath = null;
        }

        return Task.CompletedTask;
    }

    Task OnDragEntered(DragEvent e)
    {
        state.CurrentDragOveredPath = e.currentTarget.id;
        return Task.CompletedTask;
    }

    Task OnDragStarted(DragEvent e)
    {
        state.DragStartedTreeItemPath = e.currentTarget.id;

        return Task.CompletedTask;
    }

    Task OnDroped(DragEvent e)
    {
        var treeItemPathTarget = e.currentTarget.id;

        if (treeItemPathTarget != state.DragStartedTreeItemPath)
        {
            DispatchEvent(TreeItemMove, [state.DragStartedTreeItemPath, treeItemPathTarget, state.DragPosition]);
        }

        state.CurrentDragOveredPath = null;

        state.DragStartedTreeItemPath = null;

        state.CollapsedNodes.Clear();

        return Task.CompletedTask;
    }

    [DebounceTimeout(300)]
    Task OnMouseEnterHandler(MouseEvent e)
    {
        var selectedPath = e.currentTarget.id;

        DispatchEvent(TreeItemHover, [selectedPath]);

        return Task.CompletedTask;
    }

    [DebounceTimeout(300)]
    Task OnMouseLeaveHandler(MouseEvent e)
    {
        DispatchEvent(MouseLeave, []);

        return Task.CompletedTask;
    }

    Task OnTreeItemClicked(MouseEvent e)
    {
        var selectedPath = e.currentTarget.id;

        DispatchEvent(SelectionChanged, [selectedPath]);

        return Task.CompletedTask;
    }

    Task Toggle_HideInDesigner(MouseEvent e)
    {
        DispatchEvent(OnHideInDesignerToggle, []);

        return Task.CompletedTask;
    }

    Task ToggleDragPositionAfter(DragEvent e)
    {
        if (state.DragPosition == DragPosition.After)
        {
            state.DragPosition = DragPosition.Inside;
        }
        else
        {
            state.DragPosition = DragPosition.After;
        }

        return Task.CompletedTask;
    }

    Task ToggleDragPositionBefore(DragEvent e)
    {
        if (state.DragPosition == DragPosition.Before)
        {
            state.DragPosition = DragPosition.Inside;
        }
        else
        {
            state.DragPosition = DragPosition.Before;
        }

        return Task.CompletedTask;
    }

    [StopPropagation]
    Task ToggleFold(MouseEvent e)
    {
        var nodePath = e.currentTarget.id;

        if (state.CollapsedNodes.Contains(nodePath))
        {
            state.CollapsedNodes.Remove(nodePath);
        }
        else
        {
            state.CollapsedNodes.Add(nodePath);
        }

        return Task.CompletedTask;
    }

    IReadOnlyList<Element> ToVisual(VisualElementModel node, int indent, string path)
    {
        var isSelected = SelectedPath == path;

        var isDragHoveredElement = path == state.CurrentDragOveredPath && path != state.DragStartedTreeItemPath;

        Element beforePositionElement = null;
        if (isDragHoveredElement)
        {
            beforePositionElement = new FlexRow(WidthFull, Height(6), DraggableTrue)
            {
                OnDragEnter(ToggleDragPositionBefore),
                OnDragLeave(ToggleDragPositionBefore),

                BorderBottomLeftRadius(16), BorderTopLeftRadius(16),

                When(state.DragPosition == DragPosition.Before, Background(Blue300)),

                PositionAbsolute, Top(0)
            };
        }

        Element afterPositionElement = null;
        if (isDragHoveredElement)
        {
            afterPositionElement = new FlexRow(WidthFull, Height(6), DraggableTrue)
            {
                OnDragEnter(ToggleDragPositionAfter),
                OnDragLeave(ToggleDragPositionAfter),

                BorderBottomLeftRadius(16), BorderTopLeftRadius(16),

                When(state.DragPosition == DragPosition.After, Background(Blue300)),

                PositionAbsolute, Bottom(0)
            };
        }

        var icon = calculateVisualElementIcon(node);

        var foldIcon = new FlexRowCentered(Size(16), PositionAbsolute, Top(4), Left(indent * 16 - 12), Hover(BorderRadius(36), Background(Gray50)))
        {
            new IconArrowRightOrDown { IsArrowDown = !state.CollapsedNodes.Contains(path) },

            Id(path),
            OnClick(ToggleFold)
        };
        if (path == "0" || node.HasNoChild())
        {
            foldIcon = null;
        }

        Element eyeIcon = node.HideInDesigner ? new IconEyeClose() : new IconEyeOpen();
        if (isSelected is false && node.HideInDesigner is false)
        {
            eyeIcon = null;
        }

        var returnList = new List<Element>
        {
            new FlexColumn(PaddingLeft(indent * 16), Id(path), OnClick(OnTreeItemClicked), OnMouseEnter(OnMouseEnterHandler))
            {
                PositionRelative,

                foldIcon,

                beforePositionElement,

                new FlexRow(Gap(4), AlignItemsCenter)
                {
                    MarginLeft(4), FontSize13,

                    new span { GetTagText(node.Tag) },

                    icon,

                    new FlexRow(FlexGrow(1), JustifyContentFlexEnd, PaddingRight(8))
                    {
                        eyeIcon + Width(16) + Height(16) + When(isSelected, OnClick(Toggle_HideInDesigner))
                    }
                },

                state.DragStartedTreeItemPath.HasNoValue() && isSelected ? Background(Blue100) + BorderRadius(3) : null,

                state.DragStartedTreeItemPath.HasNoValue() && !isSelected ? Hover(Background(Blue50), BorderRadius(3)) : null,

                DraggableTrue,
                OnDragStart(OnDragStarted),
                OnDragEnter(OnDragEntered),
                OnDrop(OnDroped),

                afterPositionElement
            }
        };

        if (node.HasNoChild())
        {
            return returnList;
        }

        if (state.CollapsedNodes.Contains(path))
        {
            return returnList;
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];

            returnList.AddRange(ToVisual(child, indent + 1, $"{path},{i}"));
        }

        return returnList;

        static Element calculateVisualElementIcon(VisualElementModel node)
        {
            if (node.Tag == "img")
            {
                return new IconImage() + Size(16) + Color(Gray300);
            }

            if (node.Tag == "a")
            {
                return new IconLink() + Size(16) + Color(Gray300);
            }

            if (node.HasText())
            {
                if (node.Tag[0] == 'h')
                {
                    return new IconHeader() + Size(16) + Color(Gray300);
                }

                return new IconText() + Size(16) + Color(Gray300);
            }

            if (TryReadTagAsDesignerComponentId(node).Any())
            {
                return new IconReact() + Size(16) + Color(Gray300);
            }

            var styles = node.Styles;

            var hasCol = styles.Contains("col") || styles.Contains("flex-col-centered");
            var hasRow = styles.Contains("row") || styles.Contains("flex-row-centered");

            var hasFlex = styles.Any(x => TryParseProperty(x).Is("display", "flex"));

            var hasFlexDirectionColumn = styles.Any(x => TryParseProperty(x).Is("flex-direction", "column"));

            var hasFlexDirectionRow = styles.Any(x => TryParseProperty(x).Is("flex-direction", "row"));

            var hasHeightWithConstantValue = styles.Any(x => TryParseProperty(x).Is(r => r.Value.IsDouble() && r.Name.In("h", "height")));

            var hasWidhtWithConstantValue = styles.Any(x => TryParseProperty(x).Is(r => r.Value.IsDouble() && r.Name.In("w", "width")));

            if (hasFlexDirectionColumn || hasCol)
            {
                return new IconFlexColumn() + Size(16) + Color(Gray300);
            }

            if (hasFlexDirectionRow || hasFlex || hasRow)
            {
                return new IconFlexRow() + Size(16) + Color(Gray300);
            }

            if (node.HasNoText() && styles.Count == 1 && hasHeightWithConstantValue)
            {
                return new IconSpaceVertical();
            }

            if (node.HasNoText() && styles.Count == 1 && hasWidhtWithConstantValue)
            {
                return new IconSpaceHorizontal();
            }

            return null;
        }
    }

    internal class State
    {
        public List<string> CollapsedNodes { get; init; } = [];

        public string CopiedTreeItemPath { get; set; }

        public string CurrentDragOveredPath { get; set; }

        public DragPosition DragPosition { get; set; }

        public string DragStartedTreeItemPath { get; set; }
    }
}