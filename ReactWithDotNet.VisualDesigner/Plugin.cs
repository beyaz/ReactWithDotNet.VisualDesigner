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
        return Components.AllTypes.Select(t => t.Name).ToList();
    }

    public static Element TryCreateElementForPreview(string tag)
    {
        var type = Components.AllTypes.FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            return null;
        }

        return (Element)Activator.CreateInstance(type);
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
        public static readonly IReadOnlyList<Type> AllTypes =
        [
            typeof(BTypography),
            typeof(BDigitalGrid),
            typeof(BasePage),
            typeof(BDigitalGroupView),
            typeof(BDigitalBox)
        ];

        public static IReadOnlyList<string> GetPropSuggestions(string tag)
        {
            var type = Components.AllTypes.FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
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

        sealed class BasePage : Component
        {
            public string pageTitle { get; set; }

            public static IReadOnlyList<string> GetSuggestions()
            {
                return
                [
               
                ];
            }

            protected override Element render()
            {
                return new FlexColumn()
                {
                    children  =
                    {
                        new div{ pageTitle },
                        children
                    }
                   
                };
            }
        }
        
        sealed class BDigitalGroupView : Component
        {
            public string title { get; set; }

            public static IReadOnlyList<string> GetSuggestions()
            {
                return
                [
               
                ];
            }

            protected override Element render()
            {
                return new FlexColumn()
                {
                    children  =
                    {
                        new div{ title },
                        children
                    }
                   
                };
            }
        }
        
        
        sealed class BDigitalBox : Component
        {
            public string styleContext { get; set; }
            
            public static IReadOnlyList<string> GetSuggestions()
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
                    children  = { children }
                };
            }
        }
        sealed class BDigitalGrid : Component
        {
            public bool? container { get; set; }

            public string direction { get; set; }
            public bool? item { get; set; }

            public static IReadOnlyList<string> GetSuggestions()
            {
                return
                [
                    $"{nameof(container)}: true",
                    $"{nameof(item)}: true",
                    $"{nameof(direction)}: 'column'",
                    $"{nameof(direction)}: 'row'"
                ];
            }

            protected override Element render()
            {
                return new Grid
                {
                    children  = { children },
                    direction = direction
                };
            }
        }

        sealed class BTypography : Component
        {
            public string variant { get; set; }

            public static IReadOnlyList<string> GetSuggestions()
            {
                return
                [
                    $"{nameof(variant)}: 'h1'",
                    $"{nameof(variant)}: 'h2'",
                    $"{nameof(variant)}: 'h3'",
                    $"{nameof(variant)}: 'h4'",
                    $"{nameof(variant)}: 'h5'",
                    $"{nameof(variant)}: 'h6'"
                ];
            }

            protected override Element render()
            {
                return new Typography
                {
                    children = { children },
                    variant  = variant
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