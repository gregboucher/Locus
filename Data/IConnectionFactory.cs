using System.Data;
using Microsoft.Extensions.Configuration;

namespace Locus.Data
{
    public interface IConnectionFactory
    {
        IDbConnection GetConnection();
    }
}