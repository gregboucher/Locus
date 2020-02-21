using System.Linq;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System;
using Locus.ViewModels;
using Locus.Models;

namespace Locus.Data
{
    public class Repository : IRepository
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger _logger;

        public Repository(IConnectionFactory connectionFactory, ILogger logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
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

                try
                {
                    db.Query<User, Asset, GroupOfUsers, User>
                    (sql, (user, asset, group) =>
                    {
                        asset.Status = CheckStatus(asset.Due.Date);
                        if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                        {
                            groupIndex = groups.Count();
                            groupDictionary.Add(group.Id, groupIndex);
                            group.Users = new List<User>();
                            groups.Add(group);
                        }
                        var key = new Tuple<int, int>(group.Id, user.Id);
                        if (!userDictionary.TryGetValue(key, out int userIndex))
                        {
                            userIndex = groups[groupIndex].Users.Count();
                            userDictionary.Add(key, userIndex);
                            user.Assets = new List<Asset>();
                            groups[groupIndex].Users.Add(user);
                        }
                        groups[groupIndex].Users[userIndex].Assets.Add(asset);
                        return null;
                    });
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate assignment table.");
                }
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

                try
                {
                    return db.ExecuteScalar<int>(sql);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    return -1;
                }
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

                try
                {
                    return db.ExecuteScalar<int>(sql);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    return -1;
                }
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

                try
                {
                    return db.ExecuteScalar<int>(sql);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    return -1;
                }
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
                              AND Asg.Returned IS NULL
                             RIGHT JOIN (
	                               SELECT Ast.GroupId,
                                          Ast.ModelId,
                                          MAX(CASE WHEN @userId IS NOT NULL AND Asg.UserId = @userId THEN Asg.UserId ELSE NULL END) AS [User],
                                          COUNT(*) AS Total,
                                          SUM(CASE WHEN Asg.UserId IS NULL OR Asg.Returned IS NOT NULL THEN 1 ELSE 0 END) AS Surplus
		                             FROM [dbo].[Assets] AS Ast
		                                  LEFT JOIN [dbo].[Assignments] AS Asg
		                                    ON Asg.AssetId = Ast.Id
                                           AND Asg.Returned IS NULL
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

                try
                {
                    db.Query<GroupOfModels, Model, Asset, Asset>
                    (sql, (group, model, asset) =>
                    {
                        if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                        {
                            groupIndex = groups.Count();
                            groupDictionary.Add(group.Id, groupIndex);
                            group.Models = new List<Model>();
                            groups.Add(group);
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
                    }, new { userId = id });
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate model list.");
                }
                return groups;
            }
        }

        public IEnumerable<Role> GetAllRoles()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql = @"SELECT R.Id,
                                      R.Name
                                 FROM [dbo].[Roles] AS R
                                WHERE R.Deactivated IS NULL;";
                try
                {
                    var result = db.Query<Role>(sql);
                    if (result.Any())
                    {
                        return result;
                    }
                    throw new Exception();
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate role list.");
                }
            }
        }

        public UserDetails GetUserDetails(int id)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql = @"SELECT U.[Name],
                                      U.Email,
                                      U.Phone,
                                      U.RoleId,
                                      U.Absentee,
                                      U.Comment
                                 FROM [dbo].[Users] AS U
                                WHERE U.Id = @userId;";

                try
                {
                    return db.QuerySingle<UserDetails>(sql, new { userId = id });
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate user details.");
                }
            }
        }

        public QualifiedUser CreateNewUser(UserCreatePostModel model)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {        
                string sql = @"INSERT INTO [dbo].[Users]
                               VALUES (@Name,
                                       @Absentee,
                                       @Email,
                                       @Phone,
                                       GETDATE(),
                                       @Comment,
                                       @RoleId)
                                SELECT @UserId = SCOPE_IDENTITY(),
                                       @Created = Created
							      FROM [dbo].[Users]
								 WHERE Id = SCOPE_IDENTITY();";

                var parameters = new DynamicParameters(model.UserDetails);
                parameters.Add("@UserId", 0, DbType.Int32, ParameterDirection.Output);
                parameters.Add("@Created", 0, DbType.DateTime, ParameterDirection.Output);
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        db.Execute(sql, parameters, transaction);
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog(ex);
                        throw new LocusException("Unable to create new user.");
                    }

                    var groups = new List<GroupOfAssignments>();
                    var groupDictionary = new Dictionary<int, int>();
                    foreach (var _model in model.NewAssignments)
                    {
                        AddNewAssignment(db, transaction, groupDictionary, groups, _model, parameters.Get<int>("@UserId"));
                    }
                    if (groups.Any()) {
                        transaction.Commit();
                        QualifiedUser newUser = new QualifiedUser
                        {
                            Name = model.UserDetails.Name,
                            Created = parameters.Get<DateTime>("@Created"),
                            GroupedAssignments = groups
                        };
                        return newUser;
                    }
                    LocusException exception = new LocusException(
                        @"Unfortunately, we were unable to assign any assets to this user.
                          The asset pool for the selected model(s) may have since been depleted.");
                    _logger.WriteLog(exception);
                    throw exception;
                }
            }
        }

        public QualifiedUser EditExistingUser(UserEditPostModel model)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql = @"UPDATE [dbo].[Users]
                                  SET [Name] = @Name,
                                      Absentee = @Absentee,
                                      Email = @Email,
	                                  Phone =	@Phone,
	                                  Comment = @Comment,
                                      RoleId = @RoleId,
                                      @Created = Created
                                WHERE Id = @UserId;";

                var parameters = new DynamicParameters(model.UserDetails);
                parameters.Add("@UserId", model.UserId, DbType.Int32);
                parameters.Add("@Created", 0, DbType.DateTime, ParameterDirection.Output);
                DateTime created = new DateTime();
                try
                {
                    db.Execute(sql, parameters);
                    created = parameters.Get<DateTime>("@Created");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("We were unable to modify the user's details.");
                }
                var groups = new List<GroupOfAssignments>();
                var groupDictionary = new Dictionary<int, int>();
                if (model.NewAssignments != null)
                {
                    foreach (var _model in model.NewAssignments)
                    {
                        AddNewAssignment(db, null, groupDictionary, groups, _model, model.UserId);
                    }
                }
                if (model.EditAssignments != null)
                {
                    foreach (var _model in model.EditAssignments)
                    {
                        sql = "";
                        string errorMessage = "";
                        switch (_model.Action)
                        {
                            case ActionType.Return:
                                sql = @"UPDATE [dbo].[Assignments]
                                           SET Returned = GETDATE(),
                                               @AssignmentId = Id
                                         WHERE AssetId = @AssetId
                                           AND Returned IS NULL;";
                                errorMessage = "Unable to return Asset";
                                break;
                            case ActionType.Extend:
                                sql = @"UPDATE Asg
                                           SET Due = DATEADD(DAY, M.[Period], GETDATE()),
                                               @AssignmentId = Asg.Id
                                          FROM [dbo].[Assignments] AS Asg
	                                           INNER JOIN [dbo].[Assets] AS Ast
	                                              ON Ast.Id = Asg.AssetId
	                                                 INNER JOIN [dbo].[Models] AS M
		                                                ON M.Id = Ast.ModelId
                                         WHERE Asg.AssetId = @AssetId
                                           AND Asg.Returned IS NULL;";
                                errorMessage = "Unable to extend Assignment";
                                break;
                        }

                        parameters = new DynamicParameters();
                        parameters.Add("@AssetId", _model.AssetId, DbType.String);
                        parameters.Add("@AssignmentId", -1, DbType.Int32, ParameterDirection.Output);
                        int rowsChanged = -1;
                        try
                        {
                            rowsChanged = db.Execute(sql, parameters);
                            if (rowsChanged > 0)
                            {
                                sql = @"SELECT G.Id,
                                               G.[Name],
                                               M.[Name] AS Model,
                                               I.[Name] AS Icon,
                                               Ast.Tag,
                                               Asg.Due
                                          FROM [dbo].[Assignments] AS Asg
                                               INNER JOIN [dbo].[Assets] AS Ast
                                                  ON Ast.Id = Asg.AssetId
                                                     INNER JOIN [dbo].[Groups] AS G
                                                        ON G.Id = Ast.GroupId
                                                     INNER JOIN [dbo].[Models] AS M
                                                        ON M.Id = Ast.ModelId
                                                           INNER JOIN [dbo].[Icons] as I
                                                              ON I.Id = M.IconId
                                         WHERE Asg.Id = @AssignmentId;";

                                db.Query<GroupOfAssignments, QualifiedAssignment, QualifiedAssignment>
                                (sql, (group, assignment) =>
                                {
                                    if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                                    {
                                        groupIndex = groups.Count();
                                        groupDictionary.Add(group.Id, groupIndex);
                                        group.Assignments = new List<Assignment>();
                                        groups.Add(group);
                                    }
                                    assignment.Action = _model.Action;
                                    if (_model.Action == ActionType.Return) 
                                    {
                                        ReturnedAssignment returned = new ReturnedAssignment
                                        {
                                            Model = assignment.Model,
                                            Icon = assignment.Icon,
                                            Tag = assignment.Tag
                                        };
                                        groups[groupIndex].Assignments.Add(returned);
                                        return null;
                                    }
                                    groups[groupIndex].Assignments.Add(assignment);
                                    return null;
                                }, new { AssignmentId = parameters.Get<int>("@AssignmentId") }, splitOn: "Model");
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        catch
                        {
                            sql = @"SELECT G.Id,
	                                       G.[Name],
                                           M.[Name] AS Model,
                                           I.[Name] AS Icon,
                                           Ast.Tag
                                      FROM [dbo].[Assets] AS Ast
	                                       INNER JOIN [dbo].[Groups] AS G
	                                          ON G.Id = Ast.GroupId
                                           INNER JOIN [dbo].[Models] AS M
                                              ON M.Id = Ast.ModelId
		                                         INNER JOIN [dbo].[Icons] AS I
		                                            ON I.Id = M.IconId
                                     WHERE Ast.Id = @AssetId";

                            parameters = new DynamicParameters();
                            parameters.Add("@AssetId", _model.AssetId, DbType.String);
                            try
                            {
                                db.Query<GroupOfAssignments, ErrorAssignment, ErrorAssignment>
                                (sql, (group, assignment) =>
                                {
                                    if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                                    {
                                        groupIndex = groups.Count();
                                        groupDictionary.Add(group.Id, groupIndex);
                                        group.Assignments = new List<Assignment>();
                                        groups.Add(group);
                                    }
                                    assignment.Message = errorMessage;
                                    groups[groupIndex].Assignments.Add(assignment);
                                    return null;
                                }, parameters, splitOn: "Model");
                            }
                            catch
                            {
                                throw;
                            }
                        }
                    }
                }
                QualifiedUser newUser = new QualifiedUser
                {
                    Name = model.UserDetails.Name,
                    Created = created,
                    GroupedAssignments = groups
                };
                return newUser;
            }
        }

        public void AddNewAssignment(IDbConnection db, IDbTransaction transaction, Dictionary<int, int> groupDictionary, List<GroupOfAssignments> groups, NewAssignment _model, int userId)
        {
            string sql = @"INSERT INTO [dbo].[Assignments]
                            SELECT GETDATE(),
	                               DATEADD(DAY, M.[Period], GETDATE()),
	                               NULL,
	                               @UserId,
	                               (
		                             SELECT TOP 1 Ast.Id
                                       FROM [dbo].[Assets] AS Ast
	                                        LEFT JOIN [dbo].[Assignments] AS Asg
                                              ON Ast.Id = Asg.AssetId
                                      WHERE (Asg.AssetId NOT IN
                                            (
	                                           SELECT AssetId
		                                         FROM [dbo].[Assignments]
		                                        WHERE Returned IS NULL
	                                        )
	                                     OR Asg.AssetId IS NULL)
                                        AND Ast.Deactivated IS NULL
                                        AND Ast.GroupId = @GroupId
                                        AND Ast.ModelId = @ModelId
                                      ORDER BY NEWID()
	                               )
                              FROM [dbo].[Models] AS M
                             WHERE M.Id = @ModelId

                            SELECT G.[Name], M.[Name] AS Model, I.[Name] AS Icon, Ast.Tag, Asg.Due
							  FROM [dbo].[Assignments] AS Asg
							       INNER JOIN [dbo].[Assets] AS Ast
							          ON Ast.Id = Asg.AssetId
                                         INNER JOIN [dbo].[Groups] AS G
									        ON G.Id = Ast.GroupId
								         INNER JOIN [dbo].[Models] AS M
								            ON M.Id = Ast.ModelId
                                               INNER JOIN [dbo].[Icons] as I
                                                  ON I.Id = M.IconId
							 WHERE Asg.Id = SCOPE_IDENTITY();";

            var parameters = new DynamicParameters(_model);
            parameters.Add("@UserId", userId, DbType.Int32);
            try
            {
                db.Query<GroupOfAssignments, QualifiedAssignment, QualifiedAssignment>
                (sql, (group, assignment) =>
                {
                    if (!groupDictionary.TryGetValue(_model.GroupId, out int groupIndex))
                    {
                        groupIndex = groups.Count();
                        groupDictionary.Add(_model.GroupId, groupIndex);
                        group.Assignments = new List<Assignment>();
                        groups.Add(group);
                    }
                    assignment.Action = ActionType.Assign;
                    groups[groupIndex].Assignments.Add(assignment);
                    return null;
                }, parameters, transaction, splitOn: "Model");
            }
            catch
            {
                sql = @"SELECT G.[Name],
                               M.[Name] AS Model,
                               I.[Name] AS Icon
                          FROM [dbo].[Models] AS M
                               INNER JOIN [dbo].[Icons] AS I
                                  ON I.Id = M.IconId
                               CROSS JOIN [dbo].[Groups] AS G
                         WHERE M.Id = @ModelId
                           AND G.Id = @GroupId;";

                parameters = new DynamicParameters();
                parameters.Add("@ModelId", _model.ModelId, DbType.Int32);
                parameters.Add("@GroupId", _model.GroupId, DbType.Int32);
                try
                {
                    db.Query<GroupOfAssignments, ErrorAssignment, ErrorAssignment>
                    (sql, (group, assignment) =>
                    {
                        if (!groupDictionary.TryGetValue(_model.GroupId, out int groupIndex))
                        {
                            groupIndex = groups.Count();
                            groupDictionary.Add(_model.GroupId, groupIndex);
                            group.Assignments = new List<Assignment>();
                            groups.Add(group);
                        }
                        assignment.Message = "Unable to assign Asset";
                        groups[groupIndex].Assignments.Add(assignment);
                        return null;
                    }, parameters, transaction, splitOn: "Model");
                }
                catch
                {
                    throw;
                }
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