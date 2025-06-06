using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ReactWithDotNet.VisualDesigner.DataAccess;

sealed class TableNameResolver : ITableNameResolver
{
    public string ResolveTableName(Type type)
    {
        var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        
        var tableName = tableAttribute?.Name ?? type.Name;
        
        if (Config.Database.IsSQLServer)
        {
            if (Config.Database.SchemaName is null)
            {
                throw new NotSupportedException("Database schemaName should be specify.");        
            }
            return $"{Config.Database.SchemaName}.{tableName}";
        }

        return tableName;
    }
}