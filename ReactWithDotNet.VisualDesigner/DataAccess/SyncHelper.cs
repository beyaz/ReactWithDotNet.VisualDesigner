using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace ReactWithDotNet.VisualDesigner.DataAccess;

static class SyncHelper
{
    const string SchemaName = "RVD";

    const string sqlConnection = "Server=tcp:beyaz.database.windows.net,1433;Initial Catalog=ReactVisualDesigner;Persist Security Info=False;User ID=beyaz;Password=t5U7*n_5fHJ_r-yU;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

    const string sqliteConnection = @"Data Source=C:\github\ReactWithDotNet.VisualDesigner\app.db";

    public static class From_SQLite_to_SqlServer
    {
        public static Task Transfer_From_SQLite_to_SqlServer()
        {
            return Transfer_From_SQLite_to_SqlServer(sqliteConnection, sqlConnection);
        }

        static async Task Transfer_From_SQLite_to_SqlServer(string sqliteConnection, string sqlConnection)
        {
            IDbConnection source = new SqliteConnection(sqliteConnection);

            IDbConnection target = new SqlConnection(sqlConnection);

            // fetch

            var projects = await source.GetAllAsync<ProjectEntity>();

            var components = await source.GetAllAsync<ComponentEntity>();

            var users = await source.GetAllAsync<UserEntity>();

            var componentHistories = await source.GetAllAsync<ComponentHistoryEntity>();

            var workspaces = await source.GetAllAsync<ComponentWorkspace>();

            resetDommelResolvers();

            IDbTransaction dbTransaction;
            // upload
            try
            {
                target.Open();

                dbTransaction = target.BeginTransaction();

                await target.ExecuteAsync($"""
                                           DELETE FROM {SchemaName}.ComponentWorkspace
                                           DELETE FROM {SchemaName}.[User]
                                           DELETE FROM {SchemaName}.ComponentHistory
                                           DELETE FROM {SchemaName}.Component
                                           DELETE FROM {SchemaName}.Project
                                           """, null, dbTransaction);

                await insertAll(projects.ToList());
                await insertAll(components.ToList());
                await insertAll(users.ToList());
                await insertAll(componentHistories.ToList());
                await insertAll(workspaces.ToList());

                dbTransaction.Commit();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            return;

            static void resetDommelResolvers()
            {
                var map = (ConcurrentDictionary<string, string>)typeof(Resolvers).GetField("TypeTableNameCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
                map!.Clear();

                DommelMapper.SetTableNameResolver(new TableNameResolverForSqlServer());
                DommelMapper.SetKeyPropertyResolver(new KeyPropertyResolver());
            }

            async Task insertAll<TEntity>(IReadOnlyList<TEntity> records) where TEntity : class
            {
                if (records.Count is 0)
                {
                    return;
                }

                var tableName = TableNameResolverForSqlServer.Resolve(records.First().GetType());
                if (tableName == $"{SchemaName}.User")
                {
                    tableName = $"{SchemaName}.[User]";
                }

                await target.ExecuteAsync($"SET IDENTITY_INSERT {tableName} ON;", null, dbTransaction);
                await target.InsertAllAsync(records, dbTransaction);
                await target.ExecuteAsync($"SET IDENTITY_INSERT {tableName} OFF;", null, dbTransaction);
            }
        }

        class KeyPropertyResolver : IKeyPropertyResolver
        {
            public ColumnPropertyInfo[] ResolveKeyProperties(Type type)
            {
                return [];
            }
        }

        class TableNameResolverForSqlServer : ITableNameResolver
        {
            public static string Resolve(Type type)
            {
                var tableAttribute = type.GetCustomAttribute<TableAttribute>();

                var tableName = tableAttribute?.Name ?? type.Name;

                return $"{SchemaName}.{tableName}";
            }

            public string ResolveTableName(Type type)
            {
                return Resolve(type);
            }
        }
    }

    public static class From_SqlServer_to_SQLite
    {
        public static async Task Transfer_From_SqlServer_to_SQLite()
        {
            IDbConnection source = new SqlConnection(sqlConnection);

            IDbConnection target = new SqliteConnection(sqliteConnection);

            // fetch

            var projects = await source.GetAllAsync<ProjectEntity>();

            var components = await source.GetAllAsync<ComponentEntity>();

            var users = await source.GetAllAsync<UserEntity>();

            var componentHistories = await source.GetAllAsync<ComponentHistoryEntity>();

            var workspaces = await source.GetAllAsync<ComponentWorkspace>();

            resetDommelResolvers();

            // upload
            try
            {
                await target.ExecuteAsync("""
                                          DELETE FROM ComponentWorkspace
                                          DELETE FROM User
                                          DELETE FROM ComponentHistory
                                          DELETE FROM Component
                                          DELETE FROM Project
                                          """);

                await insertAll(projects.ToList());
                await insertAll(components.ToList());
                await insertAll(users.ToList());
                await insertAll(componentHistories.ToList());
                await insertAll(workspaces.ToList());
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            return;

            static void resetDommelResolvers()
            {
                var map = (ConcurrentDictionary<string, string>)typeof(Resolvers).GetField("TypeTableNameCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
                map!.Clear();

                DommelMapper.SetTableNameResolver(new TableNameResolverForSQLite());
                DommelMapper.SetKeyPropertyResolver(new KeyPropertyResolver());
            }

            async Task insertAll<TEntity>(IReadOnlyList<TEntity> records) where TEntity : class
            {
                if (records.Count is 0)
                {
                    return;
                }

                await target.InsertAllAsync(records);
            }
        }

        class KeyPropertyResolver : IKeyPropertyResolver
        {
            public ColumnPropertyInfo[] ResolveKeyProperties(Type type)
            {
                return [];
            }
        }

        class TableNameResolverForSQLite : ITableNameResolver
        {
            public static string Resolve(Type type)
            {
                var tableAttribute = type.GetCustomAttribute<TableAttribute>();

                return tableAttribute?.Name ?? type.Name;
            }

            public string ResolveTableName(Type type)
            {
                return Resolve(type);
            }
        }
    }
}