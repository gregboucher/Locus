using System.Linq;
using Dapper;
using System.Collections.Generic;
using Locus.Data;
using System.Data;

namespace Locus.Models
{
    public class Repository : IRepository
    {
        private IConnectionFactory _connectionFactory;

        public Repository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public Asset GetAsset(int SerialNumber)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                if (SerialNumber == 0) { SerialNumber = 1; }
                string sql = @"SELECT * FROM [dbo].[Assets] AS A
                               INNER JOIN [dbo].[Models] AS M
                                   ON A.ModelId = M.Id
                               INNER JOIN [dbo].[Groups] AS G
                                   ON A.GroupId = G.Id
                               WHERE A.Serial = @SerialNumber;";
                var newAsset = db.Query<Asset, Model, Group, Asset>(sql, (asset, model, group) =>
                {
                    asset.Model = model;
                    asset.Group = group;
                    return asset;
                }, SerialNumber).FirstOrDefault();

                return newAsset;
            }
        }
    }
}