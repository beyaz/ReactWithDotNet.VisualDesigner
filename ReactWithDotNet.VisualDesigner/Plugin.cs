using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;

namespace ReactWithDotNet.VisualDesigner;

sealed record PropSuggestionScope
{
    public string TagName { get; init; }
    public Maybe<ComponentEntity> Component { get; init; }
}


static class Plugin
{
    public static Element BeforeComponentPreview(RenderPreviewScope scope, VisualElementModel visualElementModel, Element component)
    {
        return component;
    }

    public static async Task<IReadOnlyList<string>> GetPropSuggestions(PropSuggestionScope scope)
    {
        var tag = scope.TagName;
        
        if (tag == "BInput")
        {
            var items = new List<string>();
            
            items.AddRange(await Cache.AccessValue($"{nameof(Plugin)}-{tag}", async () =>
            {
                var returnList = new List<string>();
                
                foreach (var text in await GetMessagingByGroupName("POSPortal"))
                {
                    returnList.Add($"floatingLabelText: \"{text}\"");
                }

                return returnList;
            }));

            return items;
        }
        
        if (tag == "BComboBox")
        {
            var items = new List<string>();

            items.AddRange(await Cache.AccessValue($"{nameof(Plugin)}-{tag}", async () =>
            {
                var returnList = new List<string>();

                foreach (var text in await GetMessagingByGroupName("POSPortal"))
                {
                    returnList.Add($"labelText: \"{text}\"");
                }

                return returnList;
            }));

            return items;
        }


        if (tag == "BCheckBox")
        {
            var items = new List<string>();

            items.AddRange(await Cache.AccessValue($"{nameof(Plugin)}-{tag}", async () =>
            {
                var returnList = new List<string>();

                foreach (var text in await GetMessagingByGroupName("POSPortal"))
                {
                    returnList.Add($"label: \"{text}\"");
                }

                return returnList;
            }));

            return items;
        }
        
        
        if (tag == "BDigitalGroupView")
        {
            var items = new List<string>();

            items.AddRange(await Cache.AccessValue($"{nameof(Plugin)}-{tag}", async () =>
            {
                var returnList = new List<string>();

                foreach (var item in await GetMessagingByGroupName("WebBanking"))
                {
                    returnList.Add($"title: \"{item.Description}\"");
                    returnList.Add($"title: \"${item.PropertyName}${item.Description}\"");
                }

                return returnList;
            }));

            return items;
        }


        return [];
    }

    record MessagingInfo
    {
        public string PropertyName { get; init; }
        
        public string Description { get; init; }
    }
    static async Task<IReadOnlyList<MessagingInfo>> GetMessagingByGroupName(string messagingGroupName)
    {
        var returnList = new List<MessagingInfo>();
        
        var connectionString = @"Data Source=srvdev\atlas;Initial Catalog=boa;Min Pool Size=10; Max Pool Size=100;Application Name=Thriller;Integrated Security=true; TrustServerCertificate=true;";

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
            
            returnList.Add(new(){PropertyName = propertyName, Description = description});
        }
        
        reader.Close();

        return returnList;
    }
}