using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
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
                returnList.InsertRange(0, prop.Suggestions ?? []);

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