global using static ReactWithDotNet.VisualDesigner.DataAccess.Extensions;
using System.IO;

namespace ReactWithDotNet.VisualDesigner.DataAccess;

record GetComponentDataInput
{
    public int ComponentId { get; init; }

    public string UserName { get; init; }
}

record GetComponentDataOutput
{
    public ComponentEntity Component { get; init; }

    public Maybe<ComponentWorkspace> ComponentWorkspaceVersion { get; init; }
}

static class Extensions
{
    public static readonly ScopeKey<ComponentEntity> Component = nameof(Component);
    public static readonly ScopeKey<int> ComponentId = nameof(ComponentId);

    public static readonly ScopeKey<Maybe<ComponentWorkspace>> ComponentWorkspaceVersion = nameof(ComponentWorkspaceVersion);

    public static readonly ScopeKey<string> UserName = nameof(UserName);

    public static async Task<Result<GetComponentDataOutput>> GetComponentData(GetComponentDataInput input)
    {
        var output = new GetComponentDataOutput
        {
            Component        = await Store.TryGetComponent(input.ComponentId),
            ComponentWorkspaceVersion = await Store.TryGetComponentWorkspace(input.ComponentId, input.UserName)
        };

        if (output.Component is null)
        {
            return new IOException($"ComponentNotFound.{input.ComponentId}");
        }

        return output;
    }

    public static string GetRootElementAsYaml(GetComponentDataOutput x)
    {
        if (x.ComponentWorkspaceVersion.HasValue)
        {
            return x.ComponentWorkspaceVersion.Value.RootElementAsYaml;
        }

        return x.Component.RootElementAsYaml;
    }
}