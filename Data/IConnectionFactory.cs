using System.Data;

namespace Locus.Data
{
    public interface IConnectionFactory
    {
        IDbConnection GetConnection();
    }
}