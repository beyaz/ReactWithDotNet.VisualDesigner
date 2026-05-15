using System.IO;

namespace ReactWithDotNet.VisualDesigner.Views;

delegate Task ComponentSelectionChanged(int componentId);

sealed class ComponentTreeView : Component<ComponentTreeView.State>
{
    public required int ComponentId { get; init; }

    public string FilterText { get; init; }

    [CustomEvent]
    public Func<string, Task> FilterTextChanged { get; init; }

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
        if (ProjectId is 0 || ComponentId is 0)
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
                        placeholder              = "search",
                        valueBind                = () => state.FilterText,
                        valueBindDebounceTimeout = 400,
                        valueBindDebounceHandler = OnFilterTextTypeFinished,
                        autoFocus                = true,
                        style =
                        {
                            FlexGrow(1),
                            Focus(OutlineNone)
                        }
                    },
                    When
                    (
                        state.FilterText?.Length > 0,
                        () => new IconClose() +
                              Size(24) + Color(Gray300) + Hover(Color(Gray400)) +
                              OnClick(OnClearFilterTextClicked)
                    )
                },
                new div(WidthFull, BorderBottom(1, dotted, "#d9d9d9"))
            },

            new FlexColumn(Flex(1), OverflowAuto)
            {
                ToVisual(CalculateRootNode(), 0)
            }
        };
    }

    static NodeModel CalculateRootNodeFrom(IEnumerable<NodeModel> nodes)
    {
        var rootNode = new NodeModel
        {
            Path = "0"
        };

        foreach (var leaf in nodes)
        {
            EnsurePath(rootNode,leaf);

            AddLeaf(rootNode, leaf);
        }

        return rootNode;

        static void AddLeaf(NodeModel rootNode, NodeModel leafNode)
        {
            var parentNode = rootNode;

            foreach (var name in leafNode.Names.SkipLast(1))
            {
                parentNode = parentNode.Children.First(x => x.Label == name);
            }

            parentNode.Children.Add(leafNode with
            {
                Label = leafNode.Names[^1],

                Path = $"{parentNode.Path}_{parentNode.Children.Count}"
            });
        }

        static void EnsurePath(NodeModel rootNode, NodeModel leafNode)
        {
            var parentNode = rootNode;

            foreach (var name in leafNode.DesignLocation.Split('/', StringSplitOptions.RemoveEmptyEntries).SkipLast(1))
            {
                var namedChild = parentNode.Children.Find(x => x.Label == name);
                if (namedChild is not null)
                {
                    parentNode = namedChild;
                    continue;
                }

                parentNode.Children.Add(new()
                {
                    Path  = $"{parentNode.Path}_{parentNode.Children.Count}",
                    Label = name
                });

                parentNode = parentNode.Children[^1];
            }
        }
    }

    NodeModel CalculateRootNode()
    {
        return CalculateRootNodeFrom(from node in GetAllNodes() where HasMatch(node) select node);

        bool HasMatch(NodeModel node)
        {
            if (node.Label?.ContainsIgnoreCase(state.FilterText) is true)
            {
                return true;
            }

            if (HasAny(from x in node.Names where x.ContainsIgnoreCase(state.FilterText) select x))
            {
                return true;
            }

            if (node.ComponentConfig?.Name.ContainsIgnoreCase(state.FilterText) is true)
            {
                return true;
            }

            if (node.ComponentConfig?.OutputFilePath.ContainsIgnoreCase(state.FilterText) is true)
            {
                return true;
            }

            return false;
        }
    }

    IReadOnlyList<NodeModel> GetAllNodes()
    {
        return Cache.AccessValue($"{nameof(ComponentTreeView)}-{nameof(GetAllNodes)}-{ProjectId}",
            () => ListFrom(from x in GetAllComponentsInProjectFromCache(ProjectId)
                           orderby x.Config.DesignLocation
                           select CreateNode(x)));

        static NodeModel CreateNode(ComponentEntity x)
        {
            var designLocation = x.Config.DesignLocation.Replace("{name}", x.Config.Name);

            var names = designLocation.Split(['/', Path.DirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return new NodeModel
            {
                ComponentId     = x.Id,
                Names           = names,
                DesignLocation  = x.Config.DesignLocation,
                ComponentConfig = x.Config
            };
        }
    }

    Task InitializeState()
    {
        state = new()
        {
            ProjectId = ProjectId,

            CollapsedNodes = [],

            FilterText = state?.FilterText ?? FilterText
        };

        CalculateRootNode();

        return Task.CompletedTask;
    }

    Task OnClearFilterTextClicked(MouseEvent e)
    {
        state = state with
        {
            FilterText = null
        };

        DispatchEvent(FilterTextChanged, [null]);

        return Task.CompletedTask;
    }

    Task OnFilterTextTypeFinished()
    {
        CalculateRootNode();

        DispatchEvent(FilterTextChanged, [state.FilterText]);

        return Task.CompletedTask;
    }

    static NodeModel GetNodeByPath(NodeModel root, string path)
    {
        var node = root;

        foreach (var item in path.Split('_', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            node = node.Children[int.Parse(item)];
        }

        return node;
    }
    
    [StopPropagation]
    async Task OnTreeItemClicked(MouseEvent e)
    {
        var selectedPath = e.currentTarget.id;

        var node = GetNodeByPath(CalculateRootNode(), selectedPath);

        if (node.ComponentId.HasValue)
        {
            DispatchEvent(SelectionChanged, [node.ComponentId.Value]);
        }
        else
        {
            await ToggleFold(e);
        }
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
        const int paddingLength = 18;

        var foldIcon = new FlexRowCentered(Size(16), PositionAbsolute, Top(4), Left(indent * paddingLength - 12), Hover(BorderRadius(36), Background(Gray50)))
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
            new FlexRow(PaddingLeft(indent * paddingLength), Id(node.Path), OnClick(OnTreeItemClicked))
            {
                When(node.ComponentId == ComponentId, Background(Blue100), BorderRadius(3)),

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

        foreach (var child in node.Children)
        {
            returnList.AddRange(ToVisual(child, indent + 1));
        }

        return returnList;
    }

    internal record State
    {
        public required List<string> CollapsedNodes { get; init; }

        public string FilterText { get; init; }

        public int ProjectId { get; init; }

        public IReadOnlyList<NodeModel> VisibleNodes { get; init; }
    }

    internal record NodeModel
    {
        public List<NodeModel> Children { get; init; } = [];

        public int? ComponentId { get; init; }

        public string DesignLocation { get; init; }

        public string Label { get; init; }

        public IReadOnlyList<string> Names { get; init; } = [];

        public string Path { get; init; }

        public ComponentConfig ComponentConfig { get; init; }

        public bool HasNoChild()
        {
            return Children.Count == 0;
        }
    }
}