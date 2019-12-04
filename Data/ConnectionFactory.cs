using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Locus.Data
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IConfiguration _config;

        public ConnectionFactory(IConfiguration config)
        {
            _config = config;
        }

        public IDbConnection GetConnection()
        {
            var connection = new SqlConnection(_config.GetConnectionString("LocusDbConnection"));
            return connection;
        }
    }
}