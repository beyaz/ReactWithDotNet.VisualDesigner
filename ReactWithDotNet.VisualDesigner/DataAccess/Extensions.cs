global using static ReactWithDotNet.VisualDesigner.DataAccess.Extensions;
using System.ComponentModel;
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
    public static string GetRootElementAsYaml(GetComponentDataOutput x)
    {
        if (x.WorkspaceVersion.HasValue)
        {
            return x.WorkspaceVersion.Value.RootElementAsYaml;
        }

        return x.Component.RootElementAsYaml;
    }
    
    public static async Task<Result<GetComponentDataOutput>> GetComponentData(GetComponentDataInput input)
    {
        var output = new GetComponentDataOutput
        {
            Component        = await Store.TryGetComponent(input.ComponentId),
            WorkspaceVersion = await Store.TryGetComponentWorkspace(input.ComponentId ,input.UserName)
        };

        if (output.Component is null)
        {
            return new IOException($"ComponentNotFound.{input.ComponentId}");
        }

        return output;
    }
    
    
}