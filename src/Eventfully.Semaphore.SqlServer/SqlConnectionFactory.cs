using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Eventfully.Semaphore.SqlServer
{
    public class SqlConnectionFactory 
    {
        private readonly string _connectionString;
        public SqlConnectionFactory()
        {
        }

        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqlConnection Get()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
