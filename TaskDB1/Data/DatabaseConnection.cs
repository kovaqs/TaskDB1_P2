using System.Configuration;
using System.Data.SqlClient;
using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TaskDB1.Data
{
    public static class DatabaseConnection
    {
        private static readonly object InitializationLock = new object();

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(GetConnectionString());
        }

        public static void Initialize()
        {
            lock (InitializationLock)
            {
                var builder = new SqlConnectionStringBuilder(GetConnectionString());
                string directory = (string)AppDomain.CurrentDomain.GetData("DataDirectory");
                Directory.CreateDirectory(directory);
                string dataFile = Path.Combine(directory, "TaskDB.mdf");
                string logFile = Path.Combine(directory, "TaskDB_log.ldf");
                string databaseName = builder.InitialCatalog;
                builder.InitialCatalog = "master";
                builder.AttachDBFilename = string.Empty;

                using (var connection = new SqlConnection(builder.ConnectionString))
                using (var command = connection.CreateCommand())
                {
                    // CREATE DATABASE requiere SQL dinámico; se escapan los valores de los parámetros.
                    command.CommandText = @"
IF DB_ID(@DatabaseName) IS NULL
BEGIN
    DECLARE @sql nvarchar(max);
    IF @Attach = 1
        SET @sql = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName)
            + N' ON (FILENAME = N''' + REPLACE(@DataFile, '''', '''''') + N''') FOR ATTACH';
    ELSE
        SET @sql = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName)
            + N' ON PRIMARY (NAME = N''' + REPLACE(@DatabaseName, '''', '''''')
            + N''', FILENAME = N''' + REPLACE(@DataFile, '''', '''''')
            + N''') LOG ON (NAME = N''' + REPLACE(@DatabaseName + N'_log', '''', '''''')
            + N''', FILENAME = N''' + REPLACE(@LogFile, '''', '''''') + N''')';
    EXEC (@sql);
END";
                    command.Parameters.Add("@DatabaseName", SqlDbType.NVarChar, 128).Value = databaseName;
                    command.Parameters.Add("@DataFile", SqlDbType.NVarChar, 4000).Value = dataFile;
                    command.Parameters.Add("@LogFile", SqlDbType.NVarChar, 4000).Value = logFile;
                    command.Parameters.Add("@Attach", SqlDbType.Bit).Value = File.Exists(dataFile);
                    command.CommandTimeout = 60;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                using (var connection = GetConnection())
                using (var command = connection.CreateCommand())
                {
                    string schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "schema.sql");
                    command.CommandText = File.ReadAllText(schemaPath);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        private static string GetConnectionString()
        {
            var settings = ConfigurationManager.ConnectionStrings["TaskDB"];
            if (settings == null)
                throw new ConfigurationErrorsException("Falta la conexión TaskDB en App.config.");

            string directory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
            if (string.IsNullOrWhiteSpace(directory))
                directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskDB1");
            directory = Path.GetFullPath(directory);
            AppDomain.CurrentDomain.SetData("DataDirectory", directory);

            var builder = new SqlConnectionStringBuilder(settings.ConnectionString);
            // Cada carpeta usa su propio catálogo para evitar colisiones.
            using (var hash = SHA256.Create())
            {
                string suffix = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(directory.ToUpperInvariant())))
                    .Replace("-", string.Empty).Substring(0, 12);
                builder.InitialCatalog = builder.InitialCatalog + "_" + suffix;
            }
            return builder.ConnectionString;
        }
    }
}
