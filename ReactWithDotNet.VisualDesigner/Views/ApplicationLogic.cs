using System.IO;
using System.Reflection;

namespace ReactWithDotNet.VisualDesigner.Views;

static class ApplicationLogic
{
    public static async Task<Result<Unit>> CommitComponent(ApplicationState state)
    {
        ComponentEntity component;
        ComponentWorkspace userVersion;
        {
            var response = await GetComponentData(state.ComponentId,state.UserName);
            if (response.HasError)
            {
                return response.Error;
            }

            component   = response.Value.Component;
            userVersion = response.Value.ComponentWorkspaceVersion.Value;
        }

        if (userVersion is null)
        {
            return new Exception($"User ({state.UserName}) has no change to commit.");
        }

        // Check if the user version is the same as the main version
        if (component.RootElementAsYaml == SerializeToYaml(state.ComponentRootElement))
        {
            await Store.Delete(userVersion);

            return new Exception($"User ({state.UserName}) has no change to commit.");
        }

        userVersion = userVersion with
        {
            RootElementAsYaml = SerializeToYaml(state.ComponentRootElement)
        };

        await Store.Insert(new ComponentHistoryEntity
        {
            ComponentId                = component.Id,
            ConfigAsYaml               = component.ConfigAsYaml,
            ComponentRootElementAsYaml = component.RootElementAsYaml,
            UserName                   = state.UserName,
            InsertTime                 = DateTime.Now
        });

        component = component with
        {
            RootElementAsYaml = userVersion.RootElementAsYaml
        };

        await Store.Update(component);

        await Store.Delete(userVersion);

        return Unit.Value;
    }

    public static IReadOnlyList<ComponentEntity> GetAllComponentsInProjectFromCache(int projectId)
    {
        return Cache.AccessValue(nameof(GetAllComponentsInProjectFromCache) + projectId, () => Store.GetAllComponentsInProject(projectId).GetAwaiter().GetResult().ToList());
    }

    public static IReadOnlyList<ProjectEntity> GetAllProjectsCached()
    {
        return Cache.AccessValue(nameof(GetAllProjectsCached),
                                 () => Store.GetAllProjects().GetAwaiter().GetResult().ToList());
    }

    public static string GetComponentDisplayText(int projectId, int componentId)
    {
        var componentEntities = GetAllComponentsInProjectFromCache(projectId);

        var componentEntity = componentEntities.FirstOrDefault(x => x.Id == componentId);

        var componentConfig = componentEntity.Config;
        
        var directoryName = componentConfig.DesignLocation.Split("/").TakeLast(2).FirstOrDefault();

        return directoryName + "/" + GetComponentName(projectId, componentId);
    }

    public static string GetComponentName(int projectId, int componentId)
    {
        return GetAllComponentsInProjectFromCache(projectId).FirstOrDefault(x => x.Id == componentId)?.Config.Name;
    }

    public static async Task<Result<VisualElementModel>> GetComponentUserOrMainVersionAsync(int componentId, string userName)
    {
        return 
            from x in await GetComponentData(componentId, userName)
            select DeserializeFromYaml<VisualElementModel>(GetRootElementAsYaml(x));
    }

    public static ProjectConfig GetProjectConfig(int projectId)
    {
        return Cache.AccessValue($"{nameof(ProjectConfig)}:{projectId}", () =>
        {
            var configAsYaml = Store.TryGetProject(projectId).GetAwaiter().GetResult()?.ConfigAsYaml;
            if (configAsYaml.HasNoValue())
            {
                return new();
            }

            return DeserializeFromYaml<ProjectConfig>(configAsYaml);
        });
    }

    public static IReadOnlyList<string> GetProjectNames(ApplicationState state)
    {
        return GetAllProjectsCached().Select(x => x.Name).ToList();
    }

    public static async Task<Result<IReadOnlyList<SuggestionItem>>> GetPropSuggestions(int componentId, string selectedTag)
    {
        var  scope = new PropSuggestionScope
        {
            Component         = await Store.TryGetComponent(componentId),
            SelectedComponent = await TryGetComponentByTag(selectedTag),
            TagName           = await GetTagText(selectedTag)
        };


        IReadOnlyList < SuggestionItem >  pluginSuggestionItems;
        {
            var result = await Plugin.GetPropSuggestions(scope);
            if (result.HasError)
            {
                return result.Error;
            }

            pluginSuggestionItems = result.Value;
        }
        
        return new List<SuggestionItem>
        {
            pluginSuggestionItems,
            
            new()
            {
                name = Design.Text,
                jsType = JsType.String
            },
            new()
            {
                name = Design.TextPreview,
            
                jsType = JsType.String
            },
            new()
            {
                name = Design.ItemsSourceDesignTimeCount,
            
                jsType = JsType.Number,
            
                value = "3"
            },
            new()
            {
                name = Design.ItemsSource,
            
                jsType = JsType.Array
            },
            new()
            {
                name = Design.ShowIf,
            
                jsType = JsType.Boolean
            },
            new()
            {
                name = Design.HideIf,
            
                jsType = JsType.Boolean
            },
            
            scope.TagName is null ? [] :

                from htmlElementType in TryGetHtmlElementTypeByTagName(scope.TagName)
                from propertyInfo in htmlElementType.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                let jsType = propertyInfo.PropertyType.FullName switch
                {
                    var t when t == typeof(string).FullName  => JsType.String,
                    var t when t == typeof(decimal).FullName => JsType.String,

                    var t when t == typeof(long).FullName   => JsType.Number,
                    var t when t == typeof(int).FullName    => JsType.Number,
                    var t when t == typeof(short).FullName  => JsType.Number,
                    var t when t == typeof(double).FullName => JsType.Number,

                    var t when t == typeof(float).FullName => JsType.Number,

                    var t when t == typeof(bool).FullName => JsType.Boolean,

                    var t when t == typeof(DateTime).FullName => JsType.Date,

                    _ => (JsType?)null
                }
                where jsType.HasValue

                select new SuggestionItem
                {
                    jsType = jsType.Value,

                    name = propertyInfo.Name
                }
        };

    }

    public static IReadOnlyList<SuggestionItem> GetStyleAttributeNameSuggestions(int projectId)
    {
        var project = GetProjectConfig(projectId);

        return new List<SuggestionItem>
        {
            from name in project.Styles.Keys
            select new SuggestionItem()
            {
                name = name,

                isVariable = true
            },
            
            // z-index 1 to 10
            from i in Enumerable.Range(1, 10)
            select new SuggestionItem
            {
                name = "z-index",

                value = i.ToString(),

                isVariable = true
            },
            
            // gap and border-radius
            
            from number in new[] { 2, 4, 6, 8, 10, 12, 16, 20, 24, 28, 32, 36, 40 }
            from name in new[] { "gap", "border-radius" }
            select new SuggestionItem()
            {
                name = name,

                value = number + "px",

                isVariable = true
            },

            // common css suggestions
            from item in CommonCssSuggestions.Map
            from value in item.Value
            select new SuggestionItem
            {
                name = item.Key,
                
                value = value,
                
                isVariable = true
            },
            
            // c o l o r s
            from colorName in project.Colors.Select(x => x.Key)
            from name in new []{"background","color"}
            select new SuggestionItem
            {
                name = name,
                
                value = colorName,
                
                isVariable = true
            },
            
            from colorName in project.Colors.Select(x => x.Key)
            from name in new[]{"border", "border-left", "border-right", "border-top", "border-bottom"}
            select new SuggestionItem
            {
                name = name,
                
                value = $"1px solid {colorName}",
                
                isVariable = true
            },
            
            // flex-grow 1 to 12
            from i in Enumerable.Range(1, 12)
            select new SuggestionItem
            {
                name = "flex-grow",

                value = i.ToString(),

                isVariable = true
            },
            
            // r a d i u s - p a d d i n g s - m a r g i n s
            from i in Enumerable.Range(1, 48)
            where i%4 == 0
            from name in new[]
            {
                "border-radius", "border-top-left-radius", "border-top-right-radius", "border-bottom-left-radius", "border-bottom-right-radius",
                
                "padding", "padding-left", "padding-right", "padding-top", "padding-bottom",
                
                "margin", "margin-left", "margin-right", "margin-top", "margin-bottom"
            }
            select new SuggestionItem
            {
                name = name,

                value = i + "px",

                isVariable = true
            }
        };


        
    }

    public static IReadOnlyList<SuggestionItem> GetTagSuggestions(int projectId)
    {
        return new List<SuggestionItem>
        {
            from name in new List<string>
            {
                Plugin.GetTagSuggestions(),
                TagNameList,
                from x in GetAllComponentsInProjectFromCache(projectId)
                select x.GetNameWithExportFilePath()
            }
            select new SuggestionItem
            {
                name = name,

                isVariable = true
            }
        };
    }

    public static async Task<string> GetTagText(string tag)
    {
        foreach (var component in await TryGetComponentByTag(tag))
        {
            return component.Config.Name;
        }

        return tag;
    }

    public static async Task<Maybe<string>> GetUserLastAccessedProjectLocalWorkspacePath()
    {
        foreach (var user in from user in await Store.GetUserByUserName(EnvironmentUserName) orderby user.LastAccessTime descending select user)
        {
            return user.LocalWorkspacePath;
        }

        return None;
    }

    public static async Task<Result<ApplicationState>> RollbackComponent(ApplicationState state)
    {
        ComponentEntity component;
        ComponentWorkspace userVersion;
        {
            var response = await GetComponentData(state.ComponentId, state.UserName);
            if (response.HasError)
            {
                return response.Error;
            }

            component   = response.Value.Component;
            userVersion = response.Value.ComponentWorkspaceVersion.Value;
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

        await Store.Insert(new ComponentHistoryEntity
        {
            ComponentId                = component.Id,
            ConfigAsYaml               = component.ConfigAsYaml,
            ComponentRootElementAsYaml = SerializeToYaml(state.ComponentRootElement),
            InsertTime                 = DateTime.Now,
            UserName                   = state.UserName
        });

        // restore from main version
        state = state with { ComponentRootElement = component.RootElementAsYaml.AsVisualElementModel() };

        await Store.Delete(userVersion);

        return state;
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

    public static async Task<Maybe<string>> TryFindFilePathFromWebRequestPath(string requestPath)
    {
        if (requestPath.StartsWith("/wwwroot/"))
        {
            foreach (var projectLocalWorkspacePath in await GetUserLastAccessedProjectLocalWorkspacePath())
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

    public static Task<Maybe<ComponentEntity>> TryGetComponentByTag(string tag)
    {
        return Cache.AccessValue($"{nameof(TryGetComponentByTag)} :: {tag}", async () =>
        {
            foreach (var componentId in TryParseInt32(tag))
            {
                Maybe<ComponentEntity> returnValue = await Store.TryGetComponent(componentId);
                
                return returnValue;
            }

            return None;
        });
    }

    public static async Task<Result<Unit>> TrySaveComponentForUser(ApplicationState state)
    {
        var componentId = state.ComponentId;

        var userName = state.UserName;

        if (componentId <= 0 || userName.HasNoValue())
        {
            return Unit.Value;
        }

        var userVersion = await Store.TryGetComponentWorkspace(componentId, userName);
        if (userVersion is null)
        {
            var component = await Store.TryGetComponent(componentId);
            if (component is null)
            {
                return new Exception($"ComponentId ({componentId}) is not found");
            }

            if (SerializeToYaml(state.ComponentRootElement) == component.RootElementAsYaml)
            {
                return Unit.Value;
            }

            userVersion = new()
            {
                ComponentId       = componentId,
                RootElementAsYaml = SerializeToYaml(state.ComponentRootElement),
                UserName          = userName,
                LastAccessTime    = DateTime.Now
            };

            await Store.Insert(userVersion);

            return Unit.Value;
        }

        await Store.Update(userVersion with
        {
            RootElementAsYaml = SerializeToYaml(state.ComponentRootElement),
            LastAccessTime = DateTime.Now
        });

        return Unit.Value;
    }

    public static async Task UpdateLastUsageInfo(ApplicationState state)
    {
        var projectId = state.ProjectId;

        var userName = state.UserName;

        if (projectId <= 0 || userName.HasNoValue())
        {
            return;
        }

        var user = await Store.TryGetUser(projectId, userName);
        if (user is not null)
        {
            if (user.LastStateAsYaml != SerializeToYaml(state))
            {
                await Store.Update(user with
                {
                    LastAccessTime = DateTime.Now,
                    LastStateAsYaml = SerializeToYaml(state)
                });
            }

            return;
        }

        await Store.Insert(new UserEntity
        {
            UserName        = userName,
            ProjectId       = projectId,
            LastAccessTime  = DateTime.Now,
            LastStateAsYaml = SerializeToYaml(state)
        });
    }
}