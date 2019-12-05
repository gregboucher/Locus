using System.Linq;
using Dapper;
using System.Collections.Generic;
using Locus.Data;
using System.Data;

namespace Locus.Models
{
    public class Repository : IRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public Repository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IEnumerable<Assignment> TestFunc()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql = @"SELECT * FROM [dbo].[Assignments] AS Asg
                             INNER JOIN [dbo].[Users] AS U
                                 ON Asg.UserId = U.Id
	                             INNER JOIN [dbo].[Roles] AS R
                                     ON U.RoleId = R.Id

                             INNER JOIN [dbo].[Assets] AS Ast
                                 ON Asg.AssetId = Ast.Id
	                             INNER JOIN [dbo].[Models] AS M
                                     ON Ast.ModelId = M.Id
	                             INNER JOIN [dbo].[Groups] AS G
                                     ON Ast.GroupId = G.Id
                             WHERE Asg.Returned IS NULL;";

                var Dict = new Dictionary<int, int>();

                IEnumerable<Assignment> assignments = db.Query<Assignment, User, Role, Asset, Model, Group, Assignment>
                (sql, (assignment, user, role, asset, model, group) =>
                {
                    

                    
                    //User u = new User();
                    //initalise u.Assests in constructor. Probably creat custom obj.
                    //var ast = asset.Id;
                    assignment.User = user;

                    assignment.User.Role = role;
                    assignment.Asset = asset;
                    assignment.Asset.Model = model;
                    assignment.Asset.Group = group;
                    //u.Assets.Add(asset); //<-------------------------
                    return assignment;
                });

                return assignments;
            }
        }

        public IEnumerable<Assignment> GetActiveAssignments()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql = @"SELECT * FROM [dbo].[Assignments] AS Asg
                             INNER JOIN [dbo].[Users] AS U
                                 ON Asg.UserId = U.Id
	                             INNER JOIN [dbo].[Roles] AS R
                                     ON U.RoleId = R.Id

                             INNER JOIN [dbo].[Assets] AS Ast
                                 ON Asg.AssetId = Ast.Id
	                             INNER JOIN [dbo].[Models] AS M
                                     ON Ast.ModelId = M.Id
	                             INNER JOIN [dbo].[Groups] AS G
                                     ON Ast.GroupId = G.Id
                             WHERE Asg.Returned IS NULL;";

                IEnumerable<Assignment> assignments = db.Query<Assignment, User, Role, Asset, Model, Group, Assignment>
                (sql, (assignment, user, role, asset, model, group) =>
                {
                    //User u = new User();
                    //initalise u.Assests in constructor. Probably creat custom obj.
                    //var ast = asset.Id;
                    assignment.User = user;
                    
                    assignment.User.Role = role;
                    assignment.Asset = asset;
                    assignment.Asset.Model = model;
                    assignment.Asset.Group = group;
                    //u.Assets.Add(asset); //<-------------------------
                    return assignment;
                });

                return assignments;
            }
        }

        public Asset GetAsset(string SerialNumber)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql = @"SELECT * FROM [dbo].[Assets] AS A
                             INNER JOIN [dbo].[Models] AS M
                                 ON A.ModelId = M.Id
                             INNER JOIN [dbo].[Groups] AS G
                                 ON A.GroupId = G.Id
                             WHERE A.Id = @SerialNumber;";

                Asset asset = db.Query<Asset, Model, Group, Asset>(sql, (asset, model, group) =>
                {
                    asset.Model = model;
                    asset.Group = group;
                    return asset;
                }, new { SerialNumber }).FirstOrDefault();

                return asset;
            }
        }
    }
}