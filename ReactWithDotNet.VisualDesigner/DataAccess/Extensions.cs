﻿global using static ReactWithDotNet.VisualDesigner.DataAccess.Extensions;
using System.IO;

namespace ReactWithDotNet.VisualDesigner.DataAccess;

static class Extensions
{
    public static async Task<Result<(ComponentEntity Component, Maybe<ComponentWorkspace> ComponentWorkspaceVersion)>>
        GetComponentData(int componentId, string userName)
    {
        var component = await Store.TryGetComponent(componentId);

        var componentWorkspaceVersion = await Store.TryGetComponentWorkspace(componentId, userName);

        if (component is null)
        {
            return new IOException($"ComponentNotFound.{componentId}");
        }

        return (component, componentWorkspaceVersion);
    }

    public static string GetRootElementAsYaml((ComponentEntity Component, Maybe<ComponentWorkspace> ComponentWorkspaceVersion) x)
    {
        if (x.ComponentWorkspaceVersion.HasValue)
        {
            return x.ComponentWorkspaceVersion.Value.RootElementAsYaml;
        }

        return x.Component.RootElementAsYaml;
    }
}