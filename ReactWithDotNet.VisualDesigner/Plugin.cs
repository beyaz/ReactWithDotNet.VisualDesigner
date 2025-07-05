using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;

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
    
    public static Element BeforeComponentPreview(RenderPreviewScope scope, VisualElementModel visualElementModel, Element component)
    {
        return component;
    }

    public static async Task<IReadOnlyList<string>> GetPropSuggestions(PropSuggestionScope scope)
    {
        var tag = scope.TagName;
        
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
            var returnList = new List<string>();
            
            foreach (var item in stringSuggestions)
            {
                returnList.Add($"title: \"{item}\"");
            }
            
            return returnList;
        }


        return [];
    }

    record MessagingInfo
    {
        public string PropertyName { get; init; }
        
        public string Description { get; init; }
    }
    static  Task<IReadOnlyList<MessagingInfo>> GetMessagingByGroupName(string messagingGroupName)
    {
        var cacheKey = $"{nameof(GetMessagingByGroupName)} :: {messagingGroupName}";
        
        return Cache.AccessValue(cacheKey, async ()=>await getMessagingByGroupName(messagingGroupName));

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
            
                returnList.Add(new(){PropertyName = propertyName, Description = description});
            }
        
            reader.Close();

            return returnList;
        }
    }
}