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
            ]
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
    }
}