using System.Collections.Immutable;
using System.Data;
using System.IO;
using Dapper.Contrib.Extensions;

namespace ReactWithDotNet.VisualDesigner.Views;

static class ApplicationLogic
{
    public static readonly ProjectConfigModel Project = DeserializeFromYaml<ProjectConfigModel>(File.ReadAllText($@"{AppDirectory}ReactWithDotNet.VisualDesigner\VisualDesigner\Project.yaml"));

    public static Task<Result> CommitComponent(ApplicationState state)
    {
        return DbOperation(async db =>
        {
            ComponentEntity userVersion;
            {
                var result = await db.GetComponentUserVersion(state.ProjectId, state.ComponentName, state.UserName);
                if (result.HasError)
                {
                    return result.Error;
                }
                userVersion = result.Value;
            }
            
            if (userVersion is null)
            {
                return Fail($"User ({state.UserName}) has no change to commit.");
            }

            ComponentEntity mainVersion;
            {
                var result = await db.GetComponentMainVersion(state.ProjectId, state.ComponentName);
                if (result.HasError)
                {
                    return result.Error;
                }

                mainVersion = result.Value;
            }
            
            if (mainVersion is null)
            {
                await db.UpdateAsync(userVersion with
                {
                    UserName = null
                });

                return Success;
            }

            // Check if the user version is the same as the main version
            if (mainVersion.RootElementAsYaml == SerializeToYaml(state.ComponentRootElement))
            {
                return Fail($"User ({state.UserName}) has no change to commit.");
            }

            userVersion = userVersion with
            {
                RootElementAsYaml = SerializeToYaml(state.ComponentRootElement)
            };

            await db.InsertAsync(new ComponentHistoryEntity
            {
                ProjectId         = mainVersion.ProjectId,
                Name              = mainVersion.Name,
                RootElementAsYaml = mainVersion.RootElementAsYaml,
                LastAccessTime    = mainVersion.LastAccessTime,
                UserName          = userVersion.UserName
            });

            mainVersion = mainVersion with
            {
                RootElementAsYaml = userVersion.RootElementAsYaml
            };

            await db.UpdateAsync(mainVersion);

            await db.DeleteAsync(userVersion);

            return Success;
        });
    }

    public static Task<Result> RollbackComponent(ApplicationState state)
    {
        return DbOperation(async db =>
        {
            ComponentEntity userVersion;
            {
                var result = await db.GetComponentUserVersion(state.ProjectId, state.ComponentName, state.UserName);
                if (result.HasError)
                {
                    return result.Error;
                }
                userVersion = result.Value;
            }
            
            if (userVersion is null)
            {
                return Fail($"User ({state.UserName}) has no change to rollback.");
            }

            ComponentEntity mainVersion;
            {
                var result = await db.GetComponentMainVersion(state.ProjectId, state.ComponentName);
                if (result.HasError)
                {
                    return result.Error;
                }

                mainVersion = result.Value;
            }
            
            if (mainVersion is null)
            {
                return Success;
            }

            // Check if the user version is the same as the main version
            if (mainVersion.RootElementAsYaml == SerializeToYaml(state.ComponentRootElement))
            {
                return Fail($"User ({state.UserName}) has no change to rollback.");
            }

            // restore from main version
            state.ComponentRootElement = mainVersion.RootElementAsYaml.AsVisualElementModel();
            
            await db.DeleteAsync(userVersion);

            return Success;
        });
    }
    
    public static Task<ImmutableList<string>> GetAllComponentNamesInProject(int projectId)
    {
        var query = $"SELECT DISTINCT({nameof(ComponentEntity.Name)}) FROM Component WHERE {nameof(ComponentEntity.ProjectId)} = @{nameof(projectId)}";

        return DbOperation(async db => (await db.QueryAsync<string>(query, new { projectId })).ToImmutableList());
    }

    public static async Task<Result<ComponentEntity>> GetComponentMainVersion(this IDbConnection db, ApplicationState state)
    {
        return await db.GetComponentMainVersion(state.ProjectId, state.ComponentName);
    }

    public static async Task<Result<ComponentEntity>> GetComponentMainVersion(this IDbConnection db, int projectId, string componentName)
    {
        if (projectId <= 0)
        {
            return new ArgumentException($"ProjectId: {projectId} is not valid");
        }

        if (componentName.HasNoValue())
        {
            return new ArgumentException($"ComponentName ({componentName}) is not valid");
        }

        const string sql = $"""
                            SELECT * 
                              FROM Component
                             WHERE {nameof(ComponentEntity.ProjectId)} = @{nameof(projectId)}
                               AND {nameof(ComponentEntity.Name)}      = @{nameof(componentName)}
                               AND {nameof(ComponentEntity.UserName)}  IS NULL OR {nameof(ComponentEntity.UserName)} = ''
                            """;

        return await db.QueryFirstOrDefaultAsync<ComponentEntity>(sql, new { projectId, componentName });
    }

    public static async Task<Result<ComponentEntity>> GetComponentUserVersion(this IDbConnection db, int projectId, string componentName, string userName)
    {
        if (projectId <= 0)
        {
            return new ArgumentException($"ProjectId: {projectId} is not valid");
        }

        if (componentName.HasNoValue())
        {
            return new ArgumentException($"ComponentName ({componentName}) is not valid");
        }

        if (userName.HasNoValue())
        {
            return new ArgumentException($"UserName ({userName}) is not valid");
        }

        const string sql = $"""
                            SELECT * 
                              FROM Component
                             WHERE {nameof(ComponentEntity.ProjectId)} = @{nameof(projectId)}
                               AND {nameof(ComponentEntity.Name)}      = @{nameof(componentName)}
                               AND {nameof(ComponentEntity.UserName)}  = @{nameof(userName)}
                            """;

        return await db.QueryFirstOrDefaultAsync<ComponentEntity>(sql, new { projectId, componentName, userName });
    }

    public static async Task<Result<ComponentEntity>> GetComponentUserVersion(this IDbConnection db, ApplicationState state)
    {
        return await db.GetComponentUserVersion(state.ProjectId, state.ComponentName, state.UserName);
    }

    public static async Task<Result<ComponentEntity>> GetComponentUserVersionNotNull(this IDbConnection db, int projectId, string componentName, string userName)
    {
        var userVersionResult = await db.GetComponentUserVersion(projectId, componentName, userName);
        if (userVersionResult.HasError)
        {
            return userVersionResult;
        }

        var userVersion = userVersionResult.Value;
        if (userVersion is not null)
        {
            return userVersion;
        }

        var mainVersionResult = await db.GetComponentMainVersion(projectId, componentName);
        if (mainVersionResult.HasError)
        {
            return mainVersionResult;
        }

        var mainVersion = mainVersionResult.Value;

        userVersion = mainVersion with
        {
            Id = 0,
            UserName = userName
        };

        await db.InsertAsync(userVersion);

        return userVersion;
    }

    public static Task<Result<ComponentEntity>> GetComponenUserOrMainVersionAsync(int projectId, string componentName, string userName)
    {
        return DbOperation(async db =>
        {
            var userVersion = await db.GetComponentUserVersion(projectId, componentName, userName);
            if (userVersion.HasError)
            {
                return userVersion;
            }

            if (userVersion.Value is not null)
            {
                return userVersion;
            }

            return await db.GetComponentMainVersion(projectId, componentName);
        });
    }
   
    public static Task<Result<ComponentEntity>> GetComponenUserOrMainVersion(ApplicationState state)
    {
        return DbOperation(async db =>
        {
            var userVersion = await db.GetComponentUserVersion(state.ProjectId, state.ComponentName, state.UserName);
            if (userVersion.HasError)
            {
                return userVersion;
            }

            if (userVersion.Value is not null)
            {
                return userVersion.Value;
            }

            return await db.GetComponentMainVersion(state.ProjectId, state.ComponentName);
        });
    }

    public static IReadOnlyList<string> GetProjectNames(ApplicationState state)
    {
        return GetAllProjects().Select(x => x.Name).ToList();
    }

    public static IReadOnlyList<string> GetPropSuggestions(ApplicationState state)
    {
        var items = new List<string>();

        string tag = null;

        if (state.Selection.VisualElementTreeItemPath.HasValue())
        {
            var selectedVisualItem = FindTreeNodeByTreePath(state.ComponentRootElement, state.Selection.VisualElementTreeItemPath);

            tag = selectedVisualItem.Tag;
        }
        
        items.Add("-text: props.userName");
        items.Add("-text: state.userName");
        items.Add("-text: 'User Name'");
        items.Add("--text: 'User Name'");
        

        if (tag == "img")
        {
            // todo: make this configurable
            const string publicFolder = @"C:\github\hopgogo\web\enduser-ui\public\";

            foreach (var pattern in new[]{"*.svg", "*.png"})
            {
                foreach (var file in Directory.GetFiles(publicFolder, pattern, SearchOption.AllDirectories))
                {
                    items.Add($"src: /{file.RemoveFromStart(publicFolder).Replace(Path.DirectorySeparatorChar,'/')}");
                }    
            }
            
            
            
           
        }

        if (tag == "a")
        {
            items.Add("href: ");
        }

        if (tag == "i")
        {
            items.Add("class: ph ph-minus");
            items.Add("class: ph ph-plus");
        
            items.Add("class: ph ph-facebook-logo");
            items.Add("class: ph ph-x-logo");
            items.Add("class: ph ph-instagram-logo");
            items.Add("class: ph ph-linkedin-logo");
        }
        
        items.Add("-items-source-design-time-count: 3");
        items.Add("-items-source: {state.userList}");
        
        items.Add("-show-if: {state.isSelectedUser}");
        items.Add("-hide-if: {state.isSelectedUser}");


        items.AddRange(GetPropsSuggestions(state));
        

        return items;
    }

    public static IReadOnlyList<string> GetStyleAttributeNameSuggestions(ApplicationState state)
    {
        var items = new List<string>();

        items.AddRange(Project.Styles.Keys);
        
        for (var i = 1; i <= 10; i++)
        {
            items.Add($"z-index: {i}");
        }
        
        for (var i = 1; i <= 100; i++)
        {
            items.Add($"border-radius: {i}");
        }

        items.Add("flex-row-centered");
        items.Add("flex-col-centered");

        foreach (var colorName in Project.Colors.Select(x => x.Key))
        {
            items.Add("color: " + colorName);
            items.Add($"border: 1px solid {colorName}");
        }
        
        items.Add("text-decoration: line-through");
        items.Add("text-decoration: underline");
        items.Add("text-decoration: overline");
        items.Add("text-decoration: none");
            
        items.Add("overflow-y: hidden");
        items.Add("overflow-y: scroll");
        items.Add("overflow-y: auto");
        items.Add("overflow-y: visible");
        
        items.Add("overflow-x: hidden");
        items.Add("overflow-x: scroll");
        items.Add("overflow-x: auto");
        items.Add("overflow-x: visible");
        
        foreach (var colorName in Project.Colors.Select(x => x.Key))
        {
            items.Add("bg: " + colorName);
        }

        // w
        {
            items.Add("w-full");
            items.Add("w-fit");
            items.Add("h-fit");
            items.Add("size-fit");
            items.Add("w-screen");
            items.Add("w-screen");
            for (var i = 1; i <= 100; i++)
            {
                if (i % 5 == 0)
                {
                    items.Add($"w-{i}vw");
                }
            }
        }

        // flex-frow
        {
            for (var i = 1; i <= 10; i++)
            {
                items.Add($"flex-grow: {i}");
            }
        }

        // paddings
        {
            string[] names = ["padding", "padding-left", "padding-right", "padding-top", "padding-bottom"];

            foreach (var name in names)
            {
                for (var i = 1; i <= 1000; i++)
                {
                    if (i % 2 == 0 || i % 5 == 0)
                    {
                        items.Add($"{name}: {i}");
                    }
                }
            }
        }
        
        // margins
        {
            string[] names = ["margin", "margin-left", "margin-right", "margin-top", "margin-bottom"];

            foreach (var name in names)
            {
                for (var i = 1; i <= 1000; i++)
                {
                    if (i % 2 == 0 || i % 5 == 0)
                    {
                        items.Add($"{name}: {i}");
                    }
                }
            }
        }

        // border
        {
            string[] names = ["border", "border-left", "border-right", "border-top", "border-bottom"];

            foreach (var name in names)
            {
                foreach (var (key, _) in Project.Colors)
                {
                    items.Add($"{name}: 1px solid {key}");
                }
            }
        }
        
        // border radius
        {
            for (var i = 1; i <= 48; i++)
            {
                items.Add($"border-top-left-radius: {i}");
                items.Add($"border-top-right-radius: {i}");
                    
                items.Add($"border-bottom-left-radius: {i}");
                items.Add($"border-bottom-right-radius: {i}");
                    
            }
        }

        foreach (var (key, values) in Project.Suggestions)
        {
            foreach (var value in values)
            {
                items.Add($"{key}: {value}");
            }
        }
        

        return items;
    }

    public static IReadOnlyList<string> GetStyleGroupConditionSuggestions(ApplicationState state)
    {
        return ["M","SM","MD","LG", "XL", "XXL", "hover", "focus", "active","visited","disabled","checked","first-child","last-child"];
    }

    public static async Task<IReadOnlyList<string>> GetSuggestionsForComponentSelection(ApplicationState state)
    {
        return (await GetAllComponentNamesInProject(state.ProjectId)).Where(name => name != state.ComponentName).ToList();
    }

    public static async Task<IReadOnlyList<string>> GetTagSuggestions(ApplicationState state)
    {
        var suggestions = new List<string>(TagNameList);

        suggestions.AddRange((await GetAllComponentNamesInProject(state.ProjectId)).Where(name => name != state.ComponentName));
        
        suggestions.Add("heroui/Checkbox");

        return suggestions;
    }

    

    public static Task<Result> TrySaveComponentForUser(ApplicationState state)
    {
        if (state.ProjectId <= 0 || state.ComponentName.HasNoValue())
        {
            return Task.FromResult(Success);
        }

        return DbOperation(async db =>
        {
            var userVersionResult = await db.GetComponentUserVersion(state.ProjectId, state.ComponentName, state.UserName);
            if (userVersionResult.HasError)
            {
                return userVersionResult.Error;
            }

            var userVersion = userVersionResult.Value;

            if (userVersion is null)
            {
                var mainVersionResult = await db.GetComponentMainVersion(state.ProjectId, state.ComponentName);
                if (mainVersionResult.HasError)
                {
                    return mainVersionResult.Error;
                }

                var mainVersion = mainVersionResult.Value;

                if (mainVersion is null)
                {
                    return new InvalidOperationException(state.ComponentName+" Main version cannot be null");
                }

                if (SerializeToYaml(state.ComponentRootElement) == mainVersion.RootElementAsYaml)
                {
                    return Success;
                }

                await db.InsertAsync(mainVersion with
                {
                    Id = 0,
                    RootElementAsYaml = SerializeToYaml(state.ComponentRootElement),
                    UserName = state.UserName
                });

                return Success;
            }

            await db.UpdateAsync(userVersion with
            {
                RootElementAsYaml = SerializeToYaml(state.ComponentRootElement)
            });

            return Success;
        });
    }

    public static Task UpdateLastUsageInfo(ApplicationState state)
    {
        if (state.ProjectId <= 0)
        {
            return Task.CompletedTask;
        }

        const string query = $"SELECT * FROM LastUsageInfo WHERE UserName = @{nameof(state.UserName)}";

        return DbOperation(async db =>
        {
            var dbRecords = await db.QueryAsync<LastUsageInfoEntity>(query, new { state.UserName });

            var dbRecord = dbRecords.FirstOrDefault(x => x.ProjectId == state.ProjectId);
            if (dbRecord is not null)
            {
                await db.UpdateAsync(dbRecord with
                {
                    LastAccessTime = DateTime.Now,
                    StateAsYaml = SerializeToYaml(state)
                });
            }
            else
            {
                await db.InsertAsync(new LastUsageInfoEntity
                {
                    UserName       = state.UserName,
                    ProjectId      = state.ProjectId,
                    LastAccessTime = DateTime.Now,
                    StateAsYaml    = SerializeToYaml(state)
                });
            }
        });
    }

    public static Task<Result> UpdateUserVersion(ApplicationState state, Func<ComponentEntity, ComponentEntity> modify)
    {
        return DbOperation(async db =>
        {
            var userVersionResult = await db.GetComponentUserVersionNotNull(state.ProjectId, state.ComponentName, state.UserName);
            if (userVersionResult.HasError)
            {
                return userVersionResult.Error;
            }

            var userVersion = userVersionResult.Value;

            userVersion = modify(userVersion);

            await db.UpdateAsync(userVersion);

            return Success;
        });
    }
}