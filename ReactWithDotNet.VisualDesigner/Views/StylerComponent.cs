namespace ReactWithDotNet.VisualDesigner.Views;


sealed class StylerComponent : Component<StylerComponent.State>
{
    protected override Task constructor()
    {
        state = new()
        {
            GroupNames =
            [
                "Layout",
                "Spacing",
                "Border",
                "Corner",
                "Typeography",
            ],
            Options = []
        };
        
        return Task.CompletedTask;
    }

    protected override Element render()
    {
        return base.render();
    }

    internal record State
    {
        public IReadOnlyList<string> GroupNames { get; init; }
        
        public IReadOnlyList<Option> Options { get; init; }
    }

    internal sealed record Option
    {
        public string Label { get; init; }
        public string Value { get; init; }
    }
}