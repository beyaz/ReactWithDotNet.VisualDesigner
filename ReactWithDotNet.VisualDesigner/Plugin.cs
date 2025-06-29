using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;

namespace ReactWithDotNet.VisualDesigner;

static class Plugin
{
    public static Element BeforeComponentPreview(RenderPreviewScope scope, VisualElementModel visualElementModel, Element component)
    {
        return component;
    }

    public static async Task<IReadOnlyList<string>> GetPropSuggestions(ApplicationState state, string tag)
    {
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


        return [];
    }

    static async Task<IReadOnlyList<string>> GetMessagingByGroupName(string messagingGroupName)
    {
        var returnList = new List<string>();
        
        var connectionString = @"Data Source=srvdev\atlas;Initial Catalog=boa;Min Pool Size=10; Max Pool Size=100;Application Name=Thriller;Integrated Security=true; TrustServerCertificate=true;";

        using IDbConnection connection = new SqlConnection(connectionString);

        const string sql = 
            """
            

                           SELECT Description
                            FROM COR.MessagingDetail WITH(NOLOCK) 
                           WHERE Code IN ( SELECT Code 
                                             FROM COR.Messaging WITH(NOLOCK)
                                            WHERE MessagingGroupId = (SELECT MessagingGroupId 
                                                                        FROM COR.MessagingGroup WITH(NOLOCK) 
                                                                       WHERE Name = @messagingGroupName))      
                                 
                           
            """;
        
        var reader = await connection.ExecuteReaderAsync(sql, new { messagingGroupName });

        while (reader.Read())
        {
            returnList.Add(reader["Description"].ToString());
        }
        
        reader.Close();

        return returnList;
    }
}