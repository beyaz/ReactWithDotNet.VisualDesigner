using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ReactWithDotNet.VisualDesigner.Configuration;

namespace ReactWithDotNet.VisualDesigner;

sealed record PropSuggestionScope
{
    public string TagName { get; init; }

    public Maybe<ComponentEntity> SelectedComponent { get; init; }

    public ComponentEntity Component { get; init; }
}

static class Plugin
{
    const string BOA_MessagingByGroupName = "BOA.MessagingByGroupName";
    const string BOA_RequestFullName = "BOA.RequestFullName";

    enum ValueTypes
    {
        String, Number, Date
    }
    
    record ComponentMeta
    {
        public IReadOnlyList<PropMeta> Props { get; init; }
        
        public string TagName { get; init; }
    }

    record PropMeta
    {
        public ValueTypes ValueType { get; init; }
        
        public string Name { get; init; }
    }

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
        }
    ];

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

        return await Cache.AccessValue($"{nameof(Plugin)}-{scope.TagName}", () =>calculate(scope));

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
        
            var numberSuggestions = new List<string>();
            {
                numberSuggestions.Add("2");
                numberSuggestions.Add("4");
                numberSuggestions.Add("8");
                numberSuggestions.Add("12");
                numberSuggestions.Add("16");
            }

            List<string> returnList = [];
            
            foreach (var prop in  from m in ComponentsMeta where m.TagName == scope.TagName from p in m.Props select p)
            {
                if (prop.ValueType == ValueTypes.String)
                {
                    foreach (var item in stringSuggestions)
                    {
                        returnList.Add($"{prop.Name}: \"{item}\"");
                    }
                }
                
                if (prop.ValueType == ValueTypes.Number)
                {
                    returnList.Add($"{prop.Name}: 2");
                    returnList.Add($"{prop.Name}: 4");
                    returnList.Add($"{prop.Name}: 8");
                    returnList.Add($"{prop.Name}: 12");
                }
            }

            return returnList;
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

    record MessagingInfo
    {
        public string PropertyName { get; init; }

        public string Description { get; init; }
    }
}