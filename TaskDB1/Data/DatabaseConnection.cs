using System.Configuration;
using System.Data.SqlClient;

namespace TaskDB.Data
{
    public static class DatabaseConnection
    {
        public static SqlConnection GetConnection()
        {
            string connectionString =
                ConfigurationManager.ConnectionStrings["TaskDB"].ConnectionString;

            return new SqlConnection(connectionString);
        }
    }
}