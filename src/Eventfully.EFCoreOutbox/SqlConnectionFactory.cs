using System;
using System.Data;
using System.Data.SqlClient;

namespace Eventfully.EFCoreOutbox
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
