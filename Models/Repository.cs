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

        public IEnumerable<Group_User> GetAssignmentsByGroup()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT
                      U.Id,
                      U.Name,
                      U.Created,
                      R.Name AS Role,
                      Ast.Tag,
                      I.Name AS Icon,
                      Asg.Due,
                      CASE
                      WHEN FORMAT(GETDATE(), 'dd-MM-yy') < FORMAT(Asg.Due, 'dd-MM-yy')
                           THEN 'Active'
                      WHEN FORMAT(GETDATE(), 'dd-MM-yy') = FORMAT(Asg.Due, 'dd-MM-yy')
                           THEN 'Due'
                           ELSE 'Overdue'
                      END AS [Status],
                      G.Id,
                      G.Name
                      FROM [dbo].[Assignments] AS Asg
                           INNER JOIN [dbo].[Users] AS U
                           ON Asg.UserId = U.Id
	                          INNER JOIN [dbo].[Roles] AS R
                              ON U.RoleId = R.Id
                           INNER JOIN [dbo].[Assets] AS Ast
                           ON Asg.AssetId = Ast.Id
	                          INNER JOIN [dbo].[Models] AS M
                              ON Ast.ModelId = M.Id
                                 INNER JOIN [dbo].[Icons] AS I
                                 ON M.IconId = I.Id
	                          INNER JOIN [dbo].[Groups] AS G
                              ON Ast.GroupId = G.Id
                     WHERE Asg.Returned IS NULL
                     ORDER BY G.Name, U.Id, Asg.Due;";

                var groups = new List<Group_User>();
                var groupDictionary = new Dictionary<int, int>();
                var userDictionary = new Dictionary<Tuple<int, int>, int>();

                db.Query<User, Asset, Group_User, User>
                (sql, (user, asset, group) =>
                {
                    if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                    {
                        groupIndex = groups.Count();
                        groupDictionary.Add(group.Id, groupIndex);
                        Group_User currentGroup = group;
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
                }, splitOn: "Tag, Id");
                return groups;
            }
        }

        public int DueTodayCount()
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
                         AND FORMAT(GETDATE(), 'dd-MM-yy') = FORMAT(Asg.Due, 'dd-MM-yy');";

                return db.ExecuteScalar<int>(sql);
            }     
        }

        public int OverdueCount()
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
                         AND FORMAT(GETDATE(), 'dd-MM-yy') > FORMAT(Asg.Due, 'dd-MM-yy');";

                return db.ExecuteScalar<int>(sql);
            }
        }

        public int UsersCreatedTodayCount()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT COUNT(DISTINCT Asg.UserId)
                        FROM [dbo].[Assignments] As Asg
                       WHERE Asg.Returned IS NULL
                         AND FORMAT(Asg.Assigned, 'dd-MM-yy') = FORMAT(GETDATE(), 'dd-MM-yy');";

                return db.ExecuteScalar<int>(sql);
            }
        }

        public IEnumerable<Group_InactiveModel> GetAllInactiveModels()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT G.Id,
                             G.Name,
	                         M.Name AS Model,
	                         I.Name AS Icon,
	                         M.Period,
                             M.Id,
	                         SUM(CASE WHEN Asg.UserId IS NULL THEN 1 ELSE 0 END) AS Surplus,
	                         COUNT(*) AS Total
                        FROM [dbo].[Assets] As Ast
	                         LEFT JOIN [dbo].[Assignments] As Asg
	                         ON Asg.AssetId = Ast.Id
	                         INNER JOIN [dbo].[Models] AS M
	                         ON Ast.ModelId = M.Id
	                            INNER JOIN [dbo].[Icons] AS I
	                            ON M.IconId = I.Id
 	                            INNER JOIN [dbo].[Groups] AS G
	                            ON Ast.GroupId = G.Id
                       WHERE Asg.Returned IS NULL
                         AND Ast.Deactivated IS NULL
                    GROUP BY G.Id, G.Name, M.Name, I.Name, M.Period, M.Id
                    ORDER BY G.Id, M.Name;";

                var groups = new List<Group_InactiveModel>();
                var groupDictionary = new Dictionary<int, int>();

                db.Query<Group_InactiveModel, InactiveModel, InactiveModel>
                (sql, (group, inactiveModel) =>
                {
                    if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                    {
                        groupIndex = groups.Count();
                        groupDictionary.Add(group.Id, groupIndex);
                        Group_InactiveModel currentGroup = group;
                        currentGroup.InactiveModels = new List<InactiveModel>();
                        groups.Add(currentGroup);
                    }
                    groups[groupIndex].InactiveModels.Add(inactiveModel);
                    groups[groupIndex].Total += inactiveModel.Total;
                    return null;
                }, splitOn: "Model");
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
    }
}