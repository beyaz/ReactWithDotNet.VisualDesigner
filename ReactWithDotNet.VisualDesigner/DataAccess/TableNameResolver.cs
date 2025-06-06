using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ReactWithDotNet.VisualDesigner.DataAccess;

sealed class TableNameResolver : ITableNameResolver
{
    public string ResolveTableName(Type type)
    {
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        var tableName = tableAttr?.Name ?? type.Name;

        if (Config.DatabaseTypeIsSqlite)
        {
            return tableName;
        }

        if (Config.DatabaseTypeIsSqlServer)
        {
            if (Config.SchemaName is null)
            {
                throw new NotSupportedException("Database schemaName should be specify.");        
            }
            return $"{Config.SchemaName}.{tableName}";
        }

        throw new NotSupportedException("Database type is not supported.");
    }
}