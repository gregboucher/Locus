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

        public IEnumerable<Group> GetAssignmentsByGroup()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql = 
                    @"SELECT Asg.Id,
                      Asg.UserId,
                      Asg.AssetId,
                      Asg.Assigned,
                      Asg.Returned,
                      U.Id,
                      U.RoleId,
                      U.Name,
                      U.Absentee,
                      U.Email,
                      U.Phone,
                      R.Id,
                      R.Name,
                      R.Deactivated,
                      Ast.Id,
                      Ast.Tag,
                      Ast.ModelId,
                      Ast.GroupId,
                      DATEADD(hour, M.[Period], Asg.Assigned) AS Due, Ast.Deactivated,
                      M.Id,
                      M.Name,
                      M.Period,
                      M.Deactivated,
                      G.Id,
                      G.Name,
                      G.Deactivated
                      FROM
                          [dbo].[Assignments] AS Asg
                      INNER JOIN
                          [dbo].[Users] AS U
                      ON Asg.UserId = U.Id
	                      INNER JOIN
                              [dbo].[Roles] AS R
                          ON U.RoleId = R.Id
                      INNER JOIN
                          [dbo].[Assets] AS Ast
                      ON Asg.AssetId = Ast.Id
	                      INNER JOIN
                              [dbo].[Models] AS M
                          ON Ast.ModelId = M.Id
	                      INNER JOIN
                              [dbo].[Groups] AS G
                          ON Ast.GroupId = G.Id
                      WHERE Asg.Returned IS NULL;";

                var assignmentsByGroup = new List<Group>();
                var groupDictionary = new Dictionary<int, int>();
                var userDictionary = new Dictionary<int, int>();
                
                db.Query<Assignment, User, Role, Asset, Model, Group, Assignment>
                (sql, (assignment, user, role, asset, model, group) =>
                {
                    User currentUser;
                    Group currentGroup;
                    asset.Model = model;
                    user.Role = role;

                    if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                    {
                        currentGroup = group;
                        currentGroup.Users = new List<User>();
                        groupIndex = assignmentsByGroup.Count();
                        assignmentsByGroup.Add(currentGroup);
                        groupDictionary.Add(group.Id, groupIndex);
                    }
                    if (!userDictionary.TryGetValue(user.Id, out int userIndex))
                    {
                        currentUser = user;
                        currentUser.Assets = new List<Asset>();
                        userIndex = assignmentsByGroup.ElementAt(groupIndex).Users.Count();
                        assignmentsByGroup.ElementAt(groupIndex).Users.Add(currentUser);
                        userDictionary.Add(user.Id, userIndex);
                    }
                    assignmentsByGroup.ElementAt(groupIndex).Users.ElementAt(userIndex).Assets.Add(asset);
                    return null;
                });
                return assignmentsByGroup;
            }
        }

        public int AssignedAssetCount()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT COUNT(Id)
                      FROM 
                          [dbo].[Assignments]
                      WHERE Returned IS NULL;";

                int count = db.ExecuteScalar<int>(sql);
                return count;
            }
        }

        public int DueTodayCount()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT COUNT(Asg.Id)
                      FROM
                          [dbo].[Assignments] AS Asg 
                      INNER JOIN
                          [dbo].[Assets] AS Ast
	                  ON Asg.AssetId = Ast.Id
	                      INNER JOIN
                              [dbo].[Models] AS M
		                  ON Ast.ModelId = M.Id
                      WHERE Returned IS NULL 
                      AND DATEDIFF(hour, GETDATE(), DATEADD(hour, M.[Period], Asg.Assigned)) BETWEEN 0 AND 24;";

                int count = db.ExecuteScalar<int>(sql);
                return count;
            }     
        }

        public int OverdueCount()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql = 
                    @"SELECT COUNT(Asg.Id)
                      FROM 
                           [dbo].[Assignments] AS Asg 
                      INNER JOIN 
                           [dbo].[Assets] AS Ast
	                  ON Asg.AssetId = Ast.Id
	                       INNER JOIN 
                               [dbo].[Models] AS M
		                   ON Ast.ModelId = M.Id
                      WHERE Returned IS NULL
                      AND DATEDIFF(hour, GETDATE(), DATEADD(hour, M.[Period], Asg.Assigned)) < 0;";

                int count = db.ExecuteScalar<int>(sql);
                return count;
            }
        }

        public Asset GetAsset(string serialNumber)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT * FROM [dbo].[Assets] AS A
                      INNER JOIN
                          [dbo].[Models] AS M
                      ON A.ModelId = M.Id
                      INNER JOIN
                          [dbo].[Groups] AS G
                      ON A.GroupId = G.Id
                      WHERE A.Id = @SerialNumber;";

                Asset asset = db.Query<Asset, Model, Group, Asset>(sql, (asset, model, group) =>
                {
                    asset.Model = model;
                    asset.Group = group;
                    return asset;
                }, new { serialNumber }).FirstOrDefault();

                return asset;
            }
        }
    }
}