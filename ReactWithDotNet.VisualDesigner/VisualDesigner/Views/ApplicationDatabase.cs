using System.Data;
using Microsoft.Data.Sqlite;

namespace ReactWithDotNet.VisualDesigner.Views;

static class ApplicationDatabase
{
   
    

   

    

    public static IReadOnlyList<ProjectEntity> GetAllProjects()
    {
        return Cache.AccessValue(nameof(GetAllProjects),
                                 () => Store.GetAllProjects().GetAwaiter().GetResult().ToList());
    }

   

}