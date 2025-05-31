using System.IO;
using System.Reflection;
using ReactWithDotNet.VisualDesigner.DataAccess;

namespace ReactWithDotNet.VisualDesigner.Views;

static class ApplicationLogic
{
    public static readonly CachedObjectMap Cache = new() { Timeout = TimeSpan.FromMinutes(5) };

    public static IReadOnlyList<ComponentEntity> GetAllComponentsInProjectFromCache(int projectId)
    {
        return Cache.AccessValue(nameof(GetAllComponentsInProjectFromCache) + projectId, () => GetAllComponentsInProject(projectId).GetAwaiter().GetResult().ToList());
    }
    
    public static string GetComponentName(int projectId, int componentId)
    {
        return GetAllComponentsInProjectFromCache(projectId).FirstOrDefault(x => x.Id == componentId).GetName();
    }
    
    public static Task<Result> CommitComponent(ApplicationState state)
    {
        return DbOperation(async db =>
        {
            ComponentEntity component;
            ComponentWorkspace userVersion;
            {
                var response = await GetComponentData(state.Map());
                if (response.HasError)
                {
                    return response.Error;
                }

                component   = response.Value.Component;
                userVersion = response.Value.WorkspaceVersion.Value;
            }

            if (userVersion is null)
            {
                return Fail($"User ({state.UserName}) has no change to commit.");
            }

            // Check if the user version is the same as the main version
            if (component.RootElementAsYaml == SerializeToYaml(state.ComponentRootElement))
            {
                return Fail($"User ({state.UserName}) has no change to commit.");
            }

            userVersion = userVersion with
            {
                RootElementAsYaml = SerializeToYaml(state.ComponentRootElement)
            };

            await db.InsertAsync(new ComponentHistoryEntity
            {
                ComponentId                = component.Id,
                ConfigAsYaml             = component.ConfigAsYaml,
                ComponentRootElementAsYaml = component.RootElementAsYaml,
                UserName                   = state.UserName,
                InsertTime                 = DateTime.Now
            });

            component = component with
            {
                RootElementAsYaml = userVersion.RootElementAsYaml
            };

            await db.UpdateAsync(component);

            await db.DeleteAsync(userVersion);

            return Success;
        });
    }

    public static Task<Result<VisualElementModel>> GetComponenUserOrMainVersionAsync(int componentId, string userName)
    {
        var input = new GetComponentDataInput { ComponentId = componentId, UserName = userName };

        return Pipe(input, GetComponentData, GetRootElementAsYaml, DeserializeFromYaml<VisualElementModel>);
    }

    public static ProjectConfig GetProjectConfig(int projectId)
    {
        return Cache.AccessValue($"{nameof(ProjectConfig)}:{projectId}", () =>
        {
            var configAsYaml = DbOperation(db => db.FirstOrDefault<ProjectEntity>(x => x.Id == projectId))?.ConfigAsYaml;
            if (configAsYaml.HasNoValue())
            {
                return new();
            }

            return DeserializeFromYaml<ProjectConfig>(configAsYaml);
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

        if (tag != null)
        {
            items.AddRange(Cache.AccessValue($"{nameof(GetPropSuggestions)}-{tag}", () =>
            {
                var returnList = new List<string>();
                foreach (var htmlElementType in TryGetHtmlElementTypeByTagName(tag))
                {
                    foreach (var propertyInfo in htmlElementType.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance))
                    {
                        returnList.Add($"{propertyInfo.Name}: {propertyInfo.PropertyType.Name}");
                    }
                }

                return returnList;
            }));
        }
       

        items.Add("-text: props.userName");
        items.Add("-text: state.userName");
        items.Add("-text: 'User Name'");
        items.Add("--text: 'User Name'");

        if (tag == "img")
        {
            items.AddRange(Cache.AccessValue("image_suggestions", () =>
            {
                var returnList = new List<string>();

                var user = GetUser(state.ProjectId, state.UserName);
                if (user is not null)
                {
                    if (user.LocalWorkspacePath.HasValue())
                    {
                        var publicFolder = Path.Combine(user.LocalWorkspacePath, "public");
                        if (Directory.Exists(publicFolder))
                        {
                            foreach (var pattern in new[] { "*.svg", "*.png" })
                            {
                                foreach (var file in Directory.GetFiles(publicFolder, pattern, SearchOption.AllDirectories))
                                {
                                    returnList.Add($"src: {file.RemoveFromStart(publicFolder).Replace(Path.DirectorySeparatorChar, '/')}");
                                }
                            }
                        }
                    }
                }

                return returnList;
            }));

            var user = GetUser(state.ProjectId, state.UserName);
            if (user is not null)
            {
                if (user.LocalWorkspacePath.HasValue())
                {
                    var publicFolder = Path.Combine(user.LocalWorkspacePath, "public");
                    if (Directory.Exists(publicFolder))
                    {
                        foreach (var pattern in new[] { "*.svg", "*.png" })
                        {
                            foreach (var file in Directory.GetFiles(publicFolder, pattern, SearchOption.AllDirectories))
                            {
                                items.Add($"src: {file.RemoveFromStart(publicFolder).Replace(Path.DirectorySeparatorChar, '/')}");
                            }
                        }
                    }
                }
            }
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

        var project = GetProjectConfig(state.ProjectId);

        items.AddRange(project.Styles.Keys);

        for (var i = 1; i <= 10; i++)
        {
            items.Add($"z-index: {i}");
        }

        foreach (var number in new[]{2,4,6,8,10,12,16,20,24,28,32,36,40})
        {
            items.Add($"gap: {number}");
        }
        
        for (var i = 1; i <= 100; i++)
        {
            items.Add($"border-radius: {i}");
        }

        items.Add("flex-row-centered");
        items.Add("flex-col-centered");

        foreach (var colorName in project.Colors.Select(x => x.Key))
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

        foreach (var colorName in project.Colors.Select(x => x.Key))
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
                foreach (var (key, _) in project.Colors)
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

        foreach (var (key, values) in project.Suggestions)
        {
            foreach (var value in values)
            {
                items.Add($"{key}: {value}");
            }
        }

        return items;
    }

    public static IReadOnlyList<string> GetTagSuggestions(ApplicationState state)
    {
        var suggestions = new List<string>(TagNameList);
        
        suggestions.AddRange(from x in GetAllComponentsInProjectFromCache(state.ProjectId) 
                             where x.Id != state.ComponentId 
                             select x.GetNameWithExportFilePath());

        return suggestions;
    }

    public static string GetTagText(string tag)
    {
        if (int.TryParse(tag, out var componentId))
        {
            var component = DbOperation(db => db.FirstOrDefault<ComponentEntity>(x => x.Id == componentId));
            if (component is null)
            {
                return tag;
            }

            return component.GetName();
        }

        return tag;
    }

    public static Maybe<string> GetUserLastAccessedProjectLocalWorkspacePath()
    {
        foreach (var user in DbOperation(db => from user in db.Select<UserEntity>(x => x.UserName == Environment.UserName) orderby user.LastAccessTime descending select user))
        {
            return user.LocalWorkspacePath;
        }

        return None;
    }

    public static async Task<Result<ApplicationState>> RollbackComponent(ApplicationState state)
    {
        return await DbOperation<Result<ApplicationState>>(async db =>
        {
            ComponentEntity component;
            ComponentWorkspace userVersion;
            {
                var response = await GetComponentData(state.Map());
                if (response.HasError)
                {
                    return response.Error;
                }

                component   = response.Value.Component;
                userVersion = response.Value.WorkspaceVersion.Value;
            }

            if (userVersion is null)
            {
                return new Exception($"User ({state.UserName}) has no change to rollback.");
            }

            // Check if the user version is the same as the main version
            if (component.RootElementAsYaml == SerializeToYaml(state.ComponentRootElement))
            {
                return new Exception($"User ({state.UserName}) has no change to rollback.");
            }

            await db.InsertAsync(new ComponentHistoryEntity
            {
                ComponentId                = component.Id,
                ConfigAsYaml              = component.ConfigAsYaml,
                ComponentRootElementAsYaml = SerializeToYaml(state.ComponentRootElement),
                InsertTime                 = DateTime.Now,
                UserName                   = state.UserName
            });

            // restore from main version
            state = state with { ComponentRootElement = component.RootElementAsYaml.AsVisualElementModel() };

            await db.DeleteAsync(userVersion);

            return state;
        });
    }

    public static async Task<Maybe<(string contentType, byte[] fileBytes)>> TryConvertLocalFilePathToFileContentResultData(string filePath)
    {
        if (File.Exists(filePath))
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".gif"            => "image/gif",
                ".svg"            => "image/svg+xml",
                _                 => "application/octet-stream"
            };

            var fileBytes = await File.ReadAllBytesAsync(filePath);

            return (contentType, fileBytes);
        }

        return None;
    }


    public static Maybe<string> TryFindFilePathFromWebRequestPath(string requestPath)
    {
        if (requestPath.StartsWith("/wwwroot/"))
        {
            foreach (var projectLocalWorkspacePath in GetUserLastAccessedProjectLocalWorkspacePath())
            {
                var filePath = Path.Combine(projectLocalWorkspacePath, "public", requestPath.RemoveFromStart("/wwwroot/"));

                if (File.Exists(filePath))
                {
                    return filePath;
                }

                return None;
            }
        }

        return None;
    }

    public static Task<Result> TrySaveComponentForUser(ApplicationState state)
    {
        var componentId = state.ComponentId;

        var userName = state.UserName;

        if (componentId <= 0 || userName.HasNoValue())
        {
            return Task.FromResult(Success);
        }

        return DbOperation(async db =>
        {
            var userVersion = await db.FirstOrDefaultAsync<ComponentWorkspace>(x => x.ComponentId == componentId && x.UserName == userName);
            if (userVersion is null)
            {
                var component = await db.FirstOrDefaultAsync<ComponentEntity>(x => x.Id == componentId);
                if (component is null)
                {
                    return new Exception($"ComponentId ({componentId}) is not found");
                }

                if (SerializeToYaml(state.ComponentRootElement) == component.RootElementAsYaml)
                {
                    return Success;
                }

                userVersion = new()
                {
                    ComponentId       = componentId,
                    RootElementAsYaml = SerializeToYaml(state.ComponentRootElement),
                    UserName          = userName,
                    LastAccessTime    = DateTime.Now
                };

                await db.InsertAsync(userVersion);

                return Success;
            }

            await db.UpdateAsync(userVersion with
            {
                RootElementAsYaml = SerializeToYaml(state.ComponentRootElement),
                LastAccessTime = DateTime.Now
            });

            return Success;
        });
    }

    public static Task UpdateLastUsageInfo(ApplicationState state)
    {
        var projectId = state.ProjectId;

        var userName = state.UserName;

        if (projectId <= 0 || userName.HasNoValue())
        {
            return Task.FromResult(Success);
        }

        return DbOperation(async db =>
        {
            var dbRecord = await db.FirstOrDefaultAsync<UserEntity>(x => x.ProjectId == projectId && x.UserName == userName);
            if (dbRecord is not null)
            {
                if (dbRecord.LastStateAsYaml != SerializeToYaml(state))
                {
                    await db.UpdateAsync(dbRecord with
                    {
                        LastAccessTime = DateTime.Now,
                        LastStateAsYaml = SerializeToYaml(state)
                    });
                }

                return;
            }

            await db.InsertAsync(new UserEntity
            {
                UserName        = userName,
                ProjectId       = projectId,
                LastAccessTime  = DateTime.Now,
                LastStateAsYaml = SerializeToYaml(state)
            });
        });
    }
}