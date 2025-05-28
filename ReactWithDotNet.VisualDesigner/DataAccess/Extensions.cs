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
    public static string GetRootElementAsYaml(GetComponentDataOutput x)
    {
        if (x.WorkspaceVersion.HasValue)
        {
            return x.WorkspaceVersion.Value.RootElementAsYaml;
        }

        return x.Component.RootElementAsYaml;
    }
    
    public static async Task<Response<GetComponentDataOutput>> GetComponentData(GetComponentDataInput input)
    {
        var output = await DbOperation(async db =>
        {
            return new GetComponentDataOutput
            {
                Component        = await db.FirstOrDefaultAsync<ComponentEntity>(x => x.Id == input.ComponentId),
                WorkspaceVersion = await db.FirstOrDefaultAsync<ComponentWorkspace>(x => x.ComponentId == input.ComponentId && x.UserName == input.UserName)
            };
        });

        if (output.Component is null)
        {
            return new IOException($"ComponentNotFound.{input.ComponentId}");
        }

        return output;
    }
    
    public static Task<ComponentWorkspace> TryGetUserVersion(int componentId, string userName)
    {
        return DbOperation(db => db.FirstOrDefaultAsync<ComponentWorkspace>(x => x.ComponentId == componentId && x.UserName == userName));
    }
}