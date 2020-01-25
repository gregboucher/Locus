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

        public IEnumerable<Group> GetAssignmentsByGroup()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT Asg.Id,
                      Asg.Assigned,
                      Asg.Due,
                      Asg.Returned,
                      Asg.UserId,
                      Asg.AssetId,
                      U.Id,
                      U.Name,
                      U.Absentee,
                      U.Email,
                      U.Phone,
                      U.Created,
                      U.Comment,
                      U.RoleId,
                      R.Id,
                      R.Name,
                      R.Deactivated,
                      Ast.Id,
                      Ast.Tag,
                      Ast.ModelId,
                      Ast.GroupId,
                      CASE
                      WHEN FORMAT(GETDATE(), 'dd-MM-yy') < FORMAT(Asg.Due, 'dd-MM-yy')
                           THEN 'Active'
                      WHEN FORMAT(GETDATE(), 'dd-MM-yy') = FORMAT(Asg.Due, 'dd-MM-yy')
                           THEN 'Due'
                           ELSE 'Overdue'
                      END AS [Status],
                      Ast.Deactivated,
                      M.Id,
                      M.Name,
                      M.Period,
                      M.IconId,
                      M.Deactivated,
                      I.Id,
                      I.Name,
                      G.Id,
                      G.Name,
                      G.Description,
                      G.Deactivated
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
                     ORDER BY G.Name, U.Id, Due;";

                var groups = new List<Group>();
                var groupDictionary = new Dictionary<int, int>();
                var userDictionary = new Dictionary<Tuple<int, int>, int>();

                db.Query<Assignment, User, Role, Asset, Model, Icon, Group, Assignment>
                (sql, (assignment, user, role, asset, model, icon, group) =>
                {
                    model.Icon = icon;
                    asset.Model = model;
                    asset.Due = assignment.Due;
                    user.Role = role;
                    
                    if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                    {
                        groupIndex = groups.Count();
                        groupDictionary.Add(group.Id, groupIndex);
                        Group currentGroup = group;
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

        public int DistinctUsersByGroupCount()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT SUM(COUNT(distinct Asg.UserId)) OVER() AS total_count
                        FROM [dbo].[Assignments] AS Asg
		                     INNER JOIN [dbo].[Assets] AS Ast
                             ON Asg.AssetId = Ast.Id
                       WHERE Asg.Returned IS NULL
                       GROUP BY Ast.GroupId";

                int count = db.ExecuteScalar<int>(sql);
                return count;
            }
        }

        public int CreatedTodayCount()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT COUNT(Asg.Id)
                        FROM [dbo].[Assignments] As Asg
                       WHERE Returned IS NULL
                         AND FORMAT(Asg.Assigned, 'dd-MM-yy') = FORMAT(GETDATE(), 'dd-MM-yy')";

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
                        FROM [dbo].[Assignments] AS Asg 
                             INNER JOIN [dbo].[Assets] AS Ast
	                         ON Asg.AssetId = Ast.Id
	                            INNER JOIN [dbo].[Models] AS M
		                        ON Ast.ModelId = M.Id
                       WHERE Returned IS NULL 
                         AND FORMAT(GETDATE(), 'dd-MM-yy') = FORMAT(Asg.Due, 'dd-MM-yy');";

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
                        FROM [dbo].[Assignments] AS Asg 
                             INNER JOIN [dbo].[Assets] AS Ast
	                         ON Asg.AssetId = Ast.Id
	                            INNER JOIN [dbo].[Models] AS M
		                        ON Ast.ModelId = M.Id
                       WHERE Returned IS NULL
                         AND FORMAT(GETDATE(), 'dd-MM-yy') > FORMAT(Asg.Due, 'dd-MM-yy');";

                int count = db.ExecuteScalar<int>(sql);
                return count;
            }
        }
    }
}