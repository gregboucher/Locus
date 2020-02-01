using System.Linq;
using Dapper;
using System.Collections.Generic;
using Locus.Data;
using System.Data;
using System;

namespace Locus.Models
{
    public class Repository : IRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public Repository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IEnumerable<GroupOfUsers> GetAssignmentsByGroup()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT U.Id,
                             U.Name,
                             U.Created,
                             R.Name AS Role,
                             Ast.Id,
                             Ast.Tag,
                             I.Name AS Icon,
                             Asg.Assigned,
                             Asg.Due,
                             G.Id,
                             G.Name
                        FROM [dbo].[Assignments] AS Asg
                             INNER JOIN [dbo].[Users] AS U
                             ON Asg.UserId = U.Id
	                            INNER JOIN [dbo].[Roles] AS R
                                ON U.RoleId = R.Id
                             INNER JOIN [dbo].[Assets] AS Ast
                             ON Asg.AssetId = Ast.Id
                                INNER JOIN [dbo].[Groups] AS G
                                ON Ast.GroupId = G.Id
	                            INNER JOIN [dbo].[Models] AS M
                                ON Ast.ModelId = M.Id
                                   INNER JOIN [dbo].[Icons] AS I
                                   ON M.IconId = I.Id
                       WHERE Asg.Returned IS NULL
                       ORDER BY G.Name, U.Id, Asg.Due;";

                var groups = new List<GroupOfUsers>();
                var groupDictionary = new Dictionary<int, int>();
                var userDictionary = new Dictionary<Tuple<int, int>, int>();

                db.Query<User, Asset, GroupOfUsers, User>
                (sql, (user, asset, group) =>
                {
                    asset.Status = CheckStatus(asset.Due.Date);
                    if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                    {
                        groupIndex = groups.Count();
                        groupDictionary.Add(group.Id, groupIndex);
                        GroupOfUsers currentGroup = group;
                        currentGroup.Users = new List<User>();
                        groups.Add(currentGroup);
                    }
                    var key = new Tuple<int, int>(group.Id, user.Id);
                    if (!userDictionary.TryGetValue(key, out int userIndex))
                    {
                        userIndex = groups[groupIndex].Users.Count();
                        userDictionary.Add(key, userIndex);
                        User currentUser = user;
                        currentUser.Assets = new List<Asset>();
                        groups[groupIndex].Users.Add(currentUser);
                    }
                    groups[groupIndex].Users[userIndex].Assets.Add(asset);
                    return null;
                });  
                return groups;
            }
        }

        public int CountDueToday()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT COUNT(*)
                        FROM [dbo].[Assignments] AS Asg 
                             INNER JOIN [dbo].[Assets] AS Ast
	                         ON Asg.AssetId = Ast.Id
	                            INNER JOIN [dbo].[Models] AS M
		                        ON Ast.ModelId = M.Id
                       WHERE Asg.Returned IS NULL 
                         AND cast(GETDATE() as date) = cast(Asg.Due as date);";

                return db.ExecuteScalar<int>(sql);
            }     
        }

        public int CountOverdue()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT COUNT(*)
                        FROM [dbo].[Assignments] AS Asg 
                             INNER JOIN [dbo].[Assets] AS Ast
	                         ON Asg.AssetId = Ast.Id
	                            INNER JOIN [dbo].[Models] AS M
		                        ON Ast.ModelId = M.Id
                       WHERE Asg.Returned IS NULL
                         AND cast(GETDATE() as date) > cast(Asg.Due as date);";

                return db.ExecuteScalar<int>(sql);
            }
        }

        public int CountUsersCreatedToday()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT COUNT(DISTINCT Asg.UserId)
                        FROM [dbo].[Assignments] As Asg
                       WHERE Asg.Returned IS NULL
                         AND cast(GETDATE() as date) = cast(Asg.Assigned as date);";

                return db.ExecuteScalar<int>(sql);
            }
        }

        public IEnumerable<GroupOfModels> GetModelsByGroup(int? id)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT Grouped.GroupId AS Id,
	                         G.[Name],
	                         Grouped.ModelId AS Id,
	                         M.[Name],
	                         I.[Name] As Icon,
	                         M.[Period],
	                         Grouped.Surplus,
	                         Grouped.Total,
	                         Asg.AssetId AS Id,
	                         Ast.Tag,
	                         Asg.Assigned,
	                         Asg.Due
                        FROM [dbo].[Assets] AS Ast
                             LEFT JOIN [dbo].[Assignments] AS Asg
                             ON Ast.Id = Asg.AssetId
                             RIGHT JOIN (
	                               SELECT Ast.GroupId,
                                          Ast.ModelId,
                                          MAX(CASE WHEN @userId IS NOT NULL AND Asg.UserId = @userId THEN Asg.UserId ELSE NULL END) AS [User],
                                          COUNT(*) AS Total,
                                          SUM(CASE WHEN Asg.UserId IS NULL THEN 1 ELSE 0 END) AS Surplus
		                             FROM [dbo].[Assets] AS Ast
		                                  LEFT JOIN [dbo].[Assignments] AS Asg
		                                  ON Asg.AssetId = Ast.Id
	                             GROUP BY Ast.GroupId, Ast.ModelId
	                         ) AS Grouped
	                         ON Grouped.[User] = Asg.UserId
                            AND Grouped.GroupId = Ast.GroupId
                            AND Grouped.ModelId = Ast.ModelId
	                            LEFT JOIN [dbo].[Groups] AS G
                                ON G.Id = Grouped.GroupId
	                            LEFT JOIN [dbo].[Models] As M
                                ON M.Id = Grouped.ModelId
	                               INNER JOIN [dbo].[Icons] AS I
                                   ON I.Id = M.IconId
                    ORDER BY Grouped.GroupId, Asg.Due DESC;";

                var groups = new List<GroupOfModels>();
                var groupDictionary = new Dictionary<int, int>();

                db.Query<GroupOfModels, Model, Asset, Asset>
                (sql, (group, model, asset) =>
                {
                    if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                    {
                        groupIndex = groups.Count();
                        groupDictionary.Add(group.Id, groupIndex);
                        GroupOfModels currentGroup = group;
                        currentGroup.Models = new List<Model>();
                        groups.Add(currentGroup);
                    }
                    if (asset != null)
                    {
                        asset.Status = CheckStatus(asset.Due.Date);
                        model.Asset = asset;
                        ++groups[groupIndex].TotalAssigned;
                    }
                    groups[groupIndex].Models.Add(model);
                    groups[groupIndex].Total += model.Total;
                    return null;
                }, new { userId = id});
                return groups;
            }
        }

        public IEnumerable<Role> GetAllRoles()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT R.Id,
                             R.Name
                        FROM [dbo].[Roles] AS R
                       WHERE R.Deactivated IS NULL;";

                return db.Query<Role>(sql);
            }
        }

        public UserDetails GetUserDetails(int id)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT U.[Name],
                             U.Email,
                             U.Phone,
                             U.RoleId,
                             U.Absentee,
                             U.Comment
                        FROM [dbo].[Users] AS U
                       WHERE U.Id = @userId;";

               return db.QuerySingle<UserDetails>(sql, new { userId = id});
            }
        }

        public string CheckStatus(DateTime dueDate)
        {
            if (DateTime.Now.Date < dueDate)
            {
                return "Active";
            }
            else if (DateTime.Now.Date == dueDate)
            {
                return "Due";
            }
            else { return "Overdue"; }
        }
    }
}