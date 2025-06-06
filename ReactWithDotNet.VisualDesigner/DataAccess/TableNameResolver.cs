using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ReactWithDotNet.VisualDesigner.DataAccess;

sealed class TableNameResolver : ITableNameResolver
{
    public string ResolveTableName(Type type)
    {
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        var tableName = tableAttr?.Name ?? type.Name;

        if (Config.Database.IsSQLite)
        {
            return tableName;
        }

        if (Config.Database.IsSQLServer)
        {
            if (Config.Database.SchemaName is null)
            {
                throw new NotSupportedException("Database schemaName should be specify.");        
            }
            return $"{Config.Database.SchemaName}.{tableName}";
        }

        throw new NotSupportedException("Database type is not supported.");
    }
}