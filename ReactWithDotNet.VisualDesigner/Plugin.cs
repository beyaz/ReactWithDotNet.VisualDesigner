using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;
using ReactWithDotNet.VisualDesigner.Configuration;

namespace ReactWithDotNet.VisualDesigner;

sealed record PropSuggestionScope
{
    public ComponentEntity Component { get; init; }

    public Maybe<ComponentEntity> SelectedComponent { get; init; }
    public string TagName { get; init; }
}

static class Plugin
{
    const string BOA_MessagingByGroupName = "BOA.MessagingByGroupName";

    static readonly IReadOnlyList<ComponentMeta> ComponentsMeta =
    [
        new()
        {
            TagName = "BasePage",
            Props =
            [
                new()
                {
                    Name      = "pageTitle",
                    ValueType = ValueTypes.String
                }
            ]
        },
        new()
        {
            TagName = "BInput",
            Props =
            [
                new()
                {
                    Name      = "floatingLabelText",
                    ValueType = ValueTypes.String
                },
                new()
                {
                    Name      = "helperText",
                    ValueType = ValueTypes.String
                },
                new()
                {
                    Name      = "maxLength",
                    ValueType = ValueTypes.Number
                },
                new()
                {
                    Name        = "autoComplete",
                    ValueType   = ValueTypes.String,
                    Suggestions = ["off", "on"]
                }
            ]
        },

        new()
        {
            TagName = "BComboBox",
            Props =
            [
                new()
                {
                    Name      = "labelText",
                    ValueType = ValueTypes.String
                }
            ]
        },

        new()
        {
            TagName = "BCheckBox",
            Props =
            [
                new()
                {
                    Name      = "label",
                    ValueType = ValueTypes.String
                }
            ]
        },

        new()
        {
            TagName = "BDigitalGroupView",
            Props =
            [
                new()
                {
                    Name      = "title",
                    ValueType = ValueTypes.String
                }
            ]
        },

        new()
        {
            TagName = "BDigitalPlateNumber",
            Props =
            [
                new()
                {
                    Name      = "label",
                    ValueType = ValueTypes.String
                }
            ]
        },
        new()
        {
            TagName = "BDigitalTabNavigator",
            Props =
            [
                new()
                {
                    Name      = "mainResource",
                    ValueType = ValueTypes.String
                },
                new()
                {
                    Name      = "selectedTab",
                    ValueType = ValueTypes.Number
                }
            ]
        },
        new()
        {
            TagName = "BDigitalDialog",
            Props =
            [
                new()
                {
                    Name      = "title",
                    ValueType = ValueTypes.String
                },
                new()
                {
                    Name      = "content",
                    ValueType = ValueTypes.String
                },
                new()
                {
                    Name      = "open",
                    ValueType = ValueTypes.Boolean
                }
            ]
        },
        new()
        {
            TagName = "BAlert",
            Props =
            [
                new()
                {
                    Name      = "severity",
                    ValueType = ValueTypes.String
                }
            ]
        },

        new()
        {
            TagName = "BDigitalBox",
            Props =
            [
                new()
                {
                    Name      = "pb",
                    ValueType = ValueTypes.Number
                }
            ]
        }
    ];

    enum ValueTypes
    {
        String,
        Number,
        Date,
        Boolean
    }

    public static ConfigModel AfterReadConfig(ConfigModel config)
    {
        if (Environment.MachineName.StartsWith("BTARC", StringComparison.OrdinalIgnoreCase))
        {
            return config with
            {
                Database = new()
                {
                    IsSQLServer      = true,
                    SchemaName       = "RVD",
                    ConnectionString = @"Data Source=srvdev\atlas;Initial Catalog=boa;Min Pool Size=10; Max Pool Size=100;Application Name=Thriller;Integrated Security=true; TrustServerCertificate=true;"
                }
            };
        }

        return config;
    }

    public static Element BeforeComponentPreview(RenderPreviewScope scope, VisualElementModel visualElementModel, Element component)
    {
        return component;
    }

    public static async Task<IReadOnlyList<string>> GetPropSuggestions(PropSuggestionScope scope)
    {
        if (scope.TagName.HasNoValue())
        {
            return [];
        }

        return await Cache.AccessValue($"{nameof(Plugin)}-{scope.TagName}", () => calculate(scope));

        static async Task<IReadOnlyList<string>> calculate(PropSuggestionScope scope)
        {
            var stringSuggestions = new List<string>();
            {
                if (scope.Component.GetConfig().TryGetValue(BOA_MessagingByGroupName, out var messagingGroupName))
                {
                    foreach (var item in await GetMessagingByGroupName(messagingGroupName))
                    {
                        stringSuggestions.Add(item.Description);
                        stringSuggestions.Add($"${item.PropertyName}$ {item.Description}");
                    }
                }
            }

            List<string> numberSuggestions =
            [
                "2",
                "4",
                "8",
                "12",
                "16",
                "24"
            ];

            List<string> dateSuggestions =
            [
                "new Date().getDate()"
            ];

            List<string> booleanSuggestions =
            [
                "true",
                "false"
            ];

            foreach (var (key, value) in scope.Component.GetConfig())
            {
                const string dotNetVariable = "DotNetVariable.";

                if (!key.StartsWith(dotNetVariable, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var variableName = key.RemoveFromStart(dotNetVariable);

                var dotnetTypeFullName = value;

                var assemblyFilePath = getAssemblyFilePathByFullTypeName(dotnetTypeFullName);
                if (assemblyFilePath is null)
                {
                    continue;
                }

                stringSuggestions.AddRange(CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsStringProperty));
                numberSuggestions.AddRange(CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsNumberProperty));
                dateSuggestions.AddRange(CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsDateTimeProperty));
                booleanSuggestions.AddRange(CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsBooleanProperty));
            }

            List<string> returnList = [];

            foreach (var prop in from m in ComponentsMeta where m.TagName == scope.TagName from p in m.Props select p)
            {
                returnList.InsertRange(0, from item in prop.Suggestions ?? [] select $"{prop.Name}: \"{item}\"");

                switch (prop.ValueType)
                {
                    case ValueTypes.String:
                    {
                        foreach (var item in stringSuggestions)
                        {
                            returnList.Add($"{prop.Name}: \"{item}\"");
                        }

                        break;
                    }
                    case ValueTypes.Number:
                    {
                        foreach (var item in numberSuggestions)
                        {
                            returnList.Add($"{prop.Name}: {item}");
                        }

                        break;
                    }
                    case ValueTypes.Date:
                    {
                        foreach (var item in dateSuggestions)
                        {
                            returnList.Add($"{prop.Name}: {item}");
                        }

                        break;
                    }
                    case ValueTypes.Boolean:
                    {
                        foreach (var item in booleanSuggestions)
                        {
                            returnList.Add($"{prop.Name}: {item}");
                        }

                        break;
                    }
                }
            }

            foreach (var item in stringSuggestions)
            {
                returnList.Add($"d-text: \"{item}\"");
            }

            returnList.InsertRange(0, Components.GetPropSuggestions(scope.TagName));

            return returnList;

            static string getAssemblyFilePathByFullTypeName(string fullTypeName)
            {
                if (fullTypeName.StartsWith("BOA.InternetBanking.Payments.API", StringComparison.OrdinalIgnoreCase))
                {
                    return @"D:\work\BOA.BusinessModules\Dev\BOA.InternetBanking.Payments\API\BOA.InternetBanking.Payments.API\bin\Debug\net8.0\BOA.InternetBanking.Payments.API.dll";
                }

                return null;
            }
        }
    }

    public static IReadOnlyList<string> GetTagSuggestions()
    {
        return Components.AllTypes.Select(x => x.type.Name).ToList();
    }

    public static Element TryCreateElementForPreview(string tag, string id, MouseEventHandler onMouseClick)
    {
        var type = Components.AllTypes.Select(x => x.type).FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            return null;
        }

        var component = (Element)Activator.CreateInstance(type);

        if (component is PluginComponentBase componentBase)
        {
            componentBase.id           = id;
            componentBase.onMouseClick = onMouseClick;
        }

        return component;
    }

    static Task<IReadOnlyList<MessagingInfo>> GetMessagingByGroupName(string messagingGroupName)
    {
        var cacheKey = $"{nameof(GetMessagingByGroupName)} :: {messagingGroupName}";

        return Cache.AccessValue(cacheKey, async () => await getMessagingByGroupName(messagingGroupName));

        static async Task<IReadOnlyList<MessagingInfo>> getMessagingByGroupName(string messagingGroupName)
        {
            var returnList = new List<MessagingInfo>();

            const string connectionString = @"Data Source=srvdev\atlas;Initial Catalog=boa;Min Pool Size=10; Max Pool Size=100;Application Name=Thriller;Integrated Security=true; TrustServerCertificate=true;";

            using IDbConnection connection = new SqlConnection(connectionString);

            const string sql =
                """
                    SELECT m.PropertyName, Description
                      FROM COR.MessagingDetail AS d WITH(NOLOCK)
                INNER JOIN COR.Messaging       AS m WITH(NOLOCK) ON d.Code = m.Code
                INNER JOIN COR.MessagingGroup  AS g WITH(NOLOCK) ON g.MessagingGroupId = m.MessagingGroupId
                     WHERE g.Name = @messagingGroupName
                       AND d.LanguageId = 1
                """;

            var reader = await connection.ExecuteReaderAsync(sql, new { messagingGroupName });

            while (reader.Read())
            {
                var propertyName = reader["PropertyName"].ToString();
                var description = reader["Description"].ToString();

                returnList.Add(new() { PropertyName = propertyName, Description = description });
            }

            reader.Close();

            return returnList;
        }
    }

    class Components
    {
        public static readonly IReadOnlyList<(Type type, Func<IReadOnlyList<string>> propSuggestions)> AllTypes =
        [
            (typeof(BTypography), BTypography.GetPropSuggestions),
            (typeof(BDigitalGrid), BDigitalGrid.GetPropSuggestions),
            (typeof(BasePage), BasePage.GetPropSuggestions),
            (typeof(BDigitalGroupView), BDigitalGroupView.GetPropSuggestions),
            (typeof(BDigitalBox), BDigitalBox.GetPropSuggestions),
            (typeof(BAlert), BAlert.GetPropSuggestions),
            (typeof(BIcon), BIcon.GetPropSuggestions)
        ];

        public static IReadOnlyList<string> GetPropSuggestions(string tag)
        {
            var type = AllTypes.Select(x => x.type).FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (type is null)
            {
                return [];
            }

            var methodInfo = type.GetMethod(nameof(GetPropSuggestions), BindingFlags.Static | BindingFlags.Public);
            if (methodInfo is null)
            {
                return [];
            }

            return (IReadOnlyList<string>)methodInfo.Invoke(null, []);
        }

        sealed class BAlert : PluginComponentBase
        {
            public string severity { get; set; }
            public string variant { get; set; }

            public static IReadOnlyList<string> GetPropSuggestions()
            {
                return
                [
                    $"{nameof(variant)}: 'standard'",
                    $"{nameof(severity)}: 'info'"
                ];
            }

            protected override Element render()
            {
                return new FlexRow(Gap(8), BackgroundColor("#e8f0fe"), Color("#1a73e8"), BorderRadius(10), Padding(12, 16), AlignItemsCenter, FontFamily("Arial, sans-serif"), FontSize14)
                {
                    Id(id), OnClick(onMouseClick),
                    new FlexRowCentered(Size(24))
                    {
                        new svg(ViewBox(0, 0, 24, 24), Fill(none), svg.Size(24))
                        {
                            new path
                            {
                                d    = "M12 17.75C12.4142 17.75 12.75 17.4142 12.75 17V11C12.75 10.5858 12.4142 10.25 12 10.25C11.5858 10.25 11.25 10.5858 11.25 11V17C11.25 17.4142 11.5858 17.75 12 17.75Z",
                                fill = "#1C274C"
                            },
                            new path
                            {
                                d    = "M12 7C12.5523 7 13 7.44772 13 8C13 8.55228 12.5523 9 12 9C11.4477 9 11 8.55228 11 8C11 7.44772 11.4477 7 12 7Z",
                                fill = "#1C274C"
                            },
                            new path
                            {
                                fillRule = "evenodd",
                                clipRule = "evenodd",
                                d        = "M1.25 12C1.25 6.06294 6.06294 1.25 12 1.25C17.9371 1.25 22.75 6.06294 22.75 12C22.75 17.9371 17.9371 22.75 12 22.75C6.06294 22.75 1.25 17.9371 1.25 12ZM12 2.75C6.89137 2.75 2.75 6.89137 2.75 12C2.75 17.1086 6.89137 21.25 12 21.25C17.1086 21.25 21.25 17.1086 21.25 12C21.25 6.89137 17.1086 2.75 12 2.75Z",
                                fill     = "#1C274C"
                            }
                        }
                    },
                    new div(Color("#202124"))
                    {
                        children
                    }
                };
            }
        }

        sealed class BasePage : PluginComponentBase
        {
            public string pageTitle { get; set; }

            public static IReadOnlyList<string> GetPropSuggestions()
            {
                return
                [
                    $"{nameof(pageTitle)}: '?'"
                ];
            }

            protected override Element render()
            {
                return new FlexColumn(Background(Gray100), WidthFull, Id(id), OnClick(onMouseClick))
                {
                    children =
                    {
                        new div(FontWeight600, FontSize18, Id(id)) { pageTitle },
                        SpaceY(24) + Id(id),
                        children
                    }
                };
            }
        }

        sealed class BDigitalBox : Component
        {
            public string styleContext { get; set; }

            public static IReadOnlyList<string> GetPropSuggestions()
            {
                return
                [
                    $"{nameof(styleContext)}: 'noMargin'"
                ];
            }

            protected override Element render()
            {
                return new Grid
                {
                    children = { children }
                };
            }
        }

        sealed class BDigitalGrid : PluginComponentBase
        {
            public string alignItems { get; set; }
            public bool? container { get; set; }

            public string direction { get; set; }

            public bool? item { get; set; }

            public string justifyContent { get; set; }

            public int? spacing { get; set; }

            public static IReadOnlyList<string> GetPropSuggestions()
            {
                return
                [
                    $"{nameof(container)}: true",
                    $"{nameof(item)}: true",
                    $"{nameof(direction)}: 'column'",
                    $"{nameof(direction)}: 'row'",

                    $"{nameof(justifyContent)}: 'flex-start'",
                    $"{nameof(justifyContent)}: 'center'",
                    $"{nameof(justifyContent)}: 'flex-end'",
                    $"{nameof(justifyContent)}: 'space-between'",
                    $"{nameof(justifyContent)}: 'space-around'",
                    $"{nameof(justifyContent)}: 'space-evenly'",

                    $"{nameof(alignItems)}: 'flex-start'",
                    $"{nameof(alignItems)}: 'stretch'",
                    $"{nameof(alignItems)}: 'flex-end'",
                    $"{nameof(alignItems)}: 'center'",
                    $"{nameof(alignItems)}: 'baseline'",

                    $"{nameof(spacing)}: 1",
                    $"{nameof(spacing)}: 2",
                    $"{nameof(spacing)}: 3",
                    $"{nameof(spacing)}: 4",
                    $"{nameof(spacing)}: 5",
                    $"{nameof(spacing)}: 6"
                ];
            }

            protected override Element render()
            {
                return new Grid
                {
                    children = { children },

                    container      = container,
                    item           = item,
                    direction      = direction,
                    justifyContent = justifyContent,
                    alignItems     = alignItems,
                    spacing        = spacing,

                    id      = id,
                    onClick = onMouseClick
                };
            }
        }

        sealed class BDigitalGroupView : PluginComponentBase
        {
            public string title { get; set; }

            public static IReadOnlyList<string> GetPropSuggestions()
            {
                return
                [
                    $"{nameof(title)}: '?'"
                ];
            }

            protected override Element render()
            {
                return new FlexColumn(Background(White), BorderRadius(8), Border(1, solid, Gray200), Padding(16), Id(id), OnClick(onMouseClick))
                {
                    children =
                    {
                        title is null ? null : new div { title },
                        children
                    }
                };
            }
        }

        sealed class BIcon : PluginComponentBase
        {
            public string name { get; set; }

            public static IReadOnlyList<string> GetPropSuggestions()
            {
                return
                [
                    $"{nameof(name)}: 'TimerRounded'",
                    $"{nameof(name)}: 'content_copy'"
                ];
            }

            protected override Element render()
            {
                return new FlexRowCentered(Size(24), Id(id), OnClick(onMouseClick))
                {
                    createSvg
                };
            }

            Element createSvg()
            {
                if (name == "TimerRounded")
                {
                    return new svg(ViewBox(0, 0, 24, 24), Fill("currentColor"), svg.Size(24))
                    {
                        new path
                        {
                            d = "M15.07 1H8.93c-.52 0-.93.41-.93.93s.41.93.93.93h6.14c.52 0 .93-.41.93-.93S15.59 1 15.07 1Zm-2.15 9.45V7.5c0-.28-.22-.5-.5-.5s-.5.22-.5.5v3.5c0 .13.05.26.15.35l2.5 2.5c.2.2.51.2.71 0 .2-.2.2-.51 0-.71l-2.36-2.36ZM12 4C7.59 4 4 7.59 4 12s3.59 8 8 8 8-3.59 8-8c0-1.9-.66-3.63-1.76-5.01l1.29-1.29c.2-.2.2-.51 0-.71s-.51-.2-.71 0l-1.3 1.3C16.63 5.21 14.39 4 12 4Zm0 14c-3.31 0-6-2.69-6-6s2.69-6 6-6 6 2.69 6 6-2.69 6-6 6Z"
                        }
                    };
                }

                if (name == "content_copy")
                {
                    return new svg(Fill("currentColor"), ViewBox(0, 0, 24, 24), svg.Size(24))
                    {
                        new path
                        {
                            d = "M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12V1Zm3 4H8c-1.1 0-2 .9-2 2v16h14c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2Zm0 18H8V7h11v16Z"
                        }
                    };
                }

                return name;
            }
        }

        sealed class BTypography : PluginComponentBase
        {
            public string variant { get; set; }

            public static IReadOnlyList<string> GetPropSuggestions()
            {
                return
                [
                    $"{nameof(variant)}: 'h1'",
                    $"{nameof(variant)}: 'h2'",
                    $"{nameof(variant)}: 'h3'",
                    $"{nameof(variant)}: 'h4'",
                    $"{nameof(variant)}: 'h5'",
                    $"{nameof(variant)}: 'h6'",

                    $"{nameof(variant)}: 'body0'",
                    $"{nameof(variant)}: 'body1'"
                ];
            }

            protected override Element render()
            {
                return new Typography
                {
                    children = { children },
                    variant  = variant,

                    id      = id,
                    onClick = onMouseClick
                };
            }
        }
    }

    record ComponentMeta
    {
        public IReadOnlyList<PropMeta> Props { get; init; }

        public string TagName { get; init; }
    }

    record PropMeta
    {
        public string Name { get; init; }

        public IReadOnlyList<string> Suggestions { get; init; }
        public ValueTypes ValueType { get; init; }
    }

    record MessagingInfo
    {
        public string Description { get; init; }
        public string PropertyName { get; init; }
    }
}