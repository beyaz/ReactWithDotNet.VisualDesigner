namespace ReactWithDotNet.VisualDesigner.Views;

delegate Task ComponentSelectionChanged(string componentName);

sealed class ComponentTreeView : Component<ComponentTreeView.State>
{
    public string ComponentName { get; init; }

    public int ProjectId { get; init; }

    [CustomEvent]
    public ComponentSelectionChanged SelectionChanged { get; init; }

    protected override Task constructor()
    {
        return InitializeState();
    }

    protected override Task OverrideStateFromPropsBeforeRender()
    {
        if (ProjectId != state.ProjectId)
        {
            return InitializeState();
        }

        return Task.CompletedTask;
    }

    protected override Element render()
    {
        if (state.RootNode is null)
        {
            return new FlexRowCentered(SizeFull) { "Empty" };
        }

        return new FlexColumn(SizeFull, CursorDefault, OutlineNone, TabIndex(0))
        {
            new FlexColumn(WidthFull)
            {
                new FlexRow(AlignItemsCenter, WidthFull, Gap(8), PaddingX(8))
                {
                    new IconFilter() + Size(16) + Color(Gray300),
                    new input
                    {
                        type                     = "text",
                        valueBind                = () => state.FilterText,
                        valueBindDebounceTimeout = 400,
                        valueBindDebounceHandler = OnFilterTextTypeFinished,
                        autoFocus                = true,
                        style =
                        {
                            FlexGrow(1),
                            Focus(OutlineNone)
                        }
                    }
                },
                new div(WidthFull, BorderBottom(1, dotted, "#d9d9d9"))
            },

            ToVisual(state.RootNode, 0)
        };
    }

    static NodeModel CalculateRootNodeFrom(IEnumerable<NodeModel> nodes)
    {
        var rootNode = new NodeModel
        {
            Path = "0"
        };

        foreach (var item in nodes)
        {
            openPath(rootNode, item.ComponentName);

            append(rootNode, item);
        }

        return rootNode;

        static void append(NodeModel rootNode, NodeModel node)
        {
            var names = node.Names.SkipLast(1).ToList();

            var parent = rootNode;

            foreach (var name in names)
            {
                parent = parent.Children.First(x => x.Label == name);
            }

            parent.Children.Add(node with { Label = node.Names.Last(), Path = $"{parent.Path}_{parent.Children.Count}" });
        }

        static void openPath(NodeModel rootNode, string componentName)
        {
            var names = componentName.Split('/', StringSplitOptions.RemoveEmptyEntries).SkipLast(1).ToList();

            var node = rootNode;

            foreach (var name in names)
            {
                var hasAlreadyNamedChild = false;

                foreach (var child in node.Children.Where(x => x.Label == name))
                {
                    node = child;

                    hasAlreadyNamedChild = true;

                    break;
                }

                if (hasAlreadyNamedChild)
                {
                    continue;
                }

                node.Children.Add(new()
                {
                    Path  = $"{node.Path}_{node.Children.Count}",
                    Label = name
                });

                node = node.Children[^1];
            }
        }
    }

    async Task CalculateRootNode()
    {
        state = state with
        {
            RootNode = CalculateRootNodeFrom((await GetAllNodes()).Where(x => x.ComponentName.Contains(state.FilterText ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
        };
    }

    async Task<IReadOnlyList<NodeModel>> GetAllNodes()
    {
        return await Cache.AccessValue($"{nameof(ComponentTreeView)}-{nameof(GetAllNodes)}-{ProjectId}", async () =>
        {
            return ListFrom(from x in await DbOperation(db => db.SelectAsync<ComponentEntity>(x => x.ProjectId == ProjectId))
                            orderby x.Name descending
                            select new NodeModel
                            {
                                ComponentId   = x.Id,
                                ComponentName = x.Name,
                                Names         = x.Name.Split('/', StringSplitOptions.RemoveEmptyEntries)
                            });
        });
    }

    async Task InitializeState()
    {
        state = new()
        {
            ProjectId = ProjectId,

            CollapsedNodes = [],

            FilterText = state?.FilterText
        };

        await CalculateRootNode();
    }

    async Task OnFilterTextTypeFinished()
    {
        await CalculateRootNode();
    }

    [StopPropagation]
    Task OnTreeItemClicked(MouseEvent e)
    {
        var selectedPath = e.currentTarget.id;

        var node = state.RootNode;

        foreach (var item in selectedPath.Split('_', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var index = int.Parse(item);

            node = node.Children[index];
        }

        if (node.ComponentName.HasValue())
        {
            DispatchEvent(SelectionChanged, [node.ComponentName]);
        }
        else
        {
            return ToggleFold(e);
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

    IReadOnlyList<Element> ToVisual(NodeModel node, int indent)
    {
        var foldIcon = new FlexRowCentered(Size(16), PositionAbsolute, Top(4), Left(indent * 16 - 12), Hover(BorderRadius(36), Background(Gray50)))
        {
            new IconArrowRightOrDown { IsArrowDown = !state.CollapsedNodes.Contains(node.Path) },

            Id(node.Path),
            OnClick(ToggleFold)
        };
        if (node.Path == "0" || node.HasNoChild())
        {
            foldIcon = null;
        }

        var returnList = new List<Element>
        {
            new FlexColumn(PaddingLeft(indent * 16), Id(node.Path), OnClick(OnTreeItemClicked))
            {
                When(node.ComponentName == ComponentName, Background(Blue100), BorderRadius(3)),

                UserSelect(none),

                PositionRelative,

                foldIcon,

                new FlexRow(Gap(4), AlignItemsCenter)
                {
                    MarginLeft(4), FontSize13,

                    new span { node.Label }
                }
            }
        };

        if (node.HasNoChild())
        {
            return returnList;
        }

        if (state.CollapsedNodes.Contains(node.Path))
        {
            return returnList;
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];

            returnList.AddRange(ToVisual(child, indent + 1));
        }

        return returnList;
    }

    internal record State
    {
        public required List<string> CollapsedNodes { get; init; }

        public string FilterText { get; init; }

        public int ProjectId { get; init; }

        public NodeModel RootNode { get; init; }

        public IReadOnlyList<NodeModel> VisibleNodes { get; init; }
    }

    internal record NodeModel
    {
        public List<NodeModel> Children { get; init; } = [];

        public int? ComponentId { get; init; }

        public string ComponentName { get; init; }

        public string Label { get; init; }

        public IReadOnlyList<string> Names { get; init; } = [];

        public string Path { get; init; }

        public bool HasNoChild()
        {
            return Children.Count == 0;
        }
    }
}