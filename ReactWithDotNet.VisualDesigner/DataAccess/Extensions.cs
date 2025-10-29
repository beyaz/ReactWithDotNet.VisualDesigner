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

    public Maybe<ComponentWorkspace> WorkspaceVersion { get; init; }
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
            WorkspaceVersion = await Store.TryGetComponentWorkspace(input.ComponentId, input.UserName)
        };

        if (output.Component is null)
        {
            return new IOException($"ComponentNotFound.{input.ComponentId}");
        }

        return output;
    }

   
    public static async Task<Result<Scope>> GetComponentData(Scope scope)
    {
        var componentId = ComponentId[scope];
        var userName = UserName[scope];

        var component = await Store.TryGetComponent(componentId);
        if (component is null)
        {
            return new IOException($"ComponentNotFound.{componentId}");
        }

        var componentWorkspaceVersion = await Store.TryGetComponentWorkspace(componentId, userName);

        return Scope.Create(new()
        {
            { Component, component },
            { ComponentWorkspaceVersion, componentWorkspaceVersion }
        });
    }

    public static string GetRootElementAsYaml(GetComponentDataOutput x)
    {
        if (x.WorkspaceVersion.HasValue)
        {
            return x.WorkspaceVersion.Value.RootElementAsYaml;
        }

        return x.Component.RootElementAsYaml;
    }
}