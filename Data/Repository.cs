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

        public IEnumerable<GroupedUsers> GetAssignmentsByGroup()
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

                var groupedUsers = new List<GroupedUsers>();
                var groupDictionary = new Dictionary<int, int>();
                var userDictionary = new Dictionary<Tuple<int, int>, int>();

                try
                {
                    db.Query<User, Asset, GroupedUsers, User>
                    (sql, (user, asset, group) =>
                    {
                        asset.Status = CheckStatus(asset.Due.Date);
                        if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                        {
                            groupIndex = groupedUsers.Count();
                            groupDictionary.Add(group.Id, groupIndex);
                            group.Users = new List<User>();
                            groupedUsers.Add(group);
                        }
                        var key = new Tuple<int, int>(group.Id, user.Id);
                        if (!userDictionary.TryGetValue(key, out int userIndex))
                        {
                            userIndex = groupedUsers[groupIndex].Users.Count();
                            userDictionary.Add(key, userIndex);
                            user.Assets = new List<Asset>();
                            groupedUsers[groupIndex].Users.Add(user);
                        }
                        groupedUsers[groupIndex].Users[userIndex].Assets.Add(asset);
                        return null;
                    });
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate assignment table.");
                }
                return groupedUsers;
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

        public IEnumerable<GroupedModels> GetModelsByGroup(int? id)
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

                var groupedModels = new List<GroupedModels>();
                var groupDictionary = new Dictionary<int, int>();

                try
                {
                    db.Query<GroupedModels, Model, Asset, Asset>
                    (sql, (group, model, asset) =>
                    {
                        if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                        {
                            groupIndex = groupedModels.Count();
                            groupDictionary.Add(group.Id, groupIndex);
                            group.Models = new List<Model>();
                            groupedModels.Add(group);
                        }
                        if (asset != null)
                        {
                            asset.Status = CheckStatus(asset.Due.Date);
                            model.Asset = asset;
                            ++groupedModels[groupIndex].TotalAssigned;
                        }
                        groupedModels[groupIndex].Models.Add(model);
                        groupedModels[groupIndex].Total += model.Total;
                        return null;
                    }, new { userId = id });
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate model list.");
                }
                return groupedModels;
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

        public UserSummary CreateNewUser(UserCreatePostModel model)
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

                    var groupedAssignments = new List<GroupedAssignments>();
                    var groupDictionary = new Dictionary<int, int>();
                    int userId = parameters.Get<int>("@UserId");
                    Boolean assetsAssigned = false;
                    foreach (var _model in model.NewSelections)
                    {
                        if (AddNewAssignment(db, transaction, groupDictionary, groupedAssignments, _model, userId))
                        {
                            assetsAssigned = true;
                        }
                    }
                    if (groupedAssignments.Any()) {
                        transaction.Commit();
                        UserSummary summary = new UserSummary
                        {
                            Id = userId,
                            Name = model.UserDetails.Name,
                            Created = parameters.Get<DateTime>("@Created"),
                            ActiveAssignments = assetsAssigned,
                            GroupedAssignments = groupedAssignments
                        };
                        return summary;
                    }
                    LocusException exception = new LocusException(
                        @"Unfortunately, we were unable to assign any assets to this user.
                          The asset pool for the selected model(s) may have since been depleted.");
                    _logger.WriteLog(exception);
                    throw exception;
                }
            }
        }

        public UserSummary EditExistingUser(UserEditPostModel postModel)
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

                var parameters = new DynamicParameters(postModel.UserDetails);
                parameters.Add("@UserId", postModel.UserId, DbType.Int32);
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
                    throw new LocusException("We were unable to edit this User.");
                }
                var groupedAssignments = new List<GroupedAssignments>();
                var groupDictionary = new Dictionary<int, int>();
                Boolean assetsAssigned = false;
                if (postModel.NewSelections != null)
                {
                    foreach (var selection in postModel.NewSelections)
                    {
                        if (AddNewAssignment(db, null, groupDictionary, groupedAssignments, selection, postModel.UserId))
                        {
                            assetsAssigned = true;
                        }
                    }
                }
                if (postModel.EditSelections != null)
                {
                    foreach (var selection in postModel.EditSelections)
                    {
                        sql = "";
                        string errorMessage = "";
                        switch (selection.Action)
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
                        parameters.Add("@AssetId", selection.AssetId, DbType.String);
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

                                db.Query<GroupedAssignments, QualifiedAssignment, QualifiedAssignment>
                                (sql, (group, assignment) =>
                                {
                                    int groupIndex = GetGroupIndex(group.Id, groupDictionary, groupedAssignments, group);
                                    assignment.Action = selection.Action;
                                    if (selection.Action == ActionType.Return) 
                                    {
                                        ReturnedAssignment returned = new ReturnedAssignment
                                        {
                                            Model = assignment.Model,
                                            Icon = assignment.Icon,
                                            Tag = assignment.Tag
                                        };
                                        groupedAssignments[groupIndex].Assignments.Add(returned);
                                        return null;
                                    }
                                    groupedAssignments[groupIndex].Assignments.Add(assignment);
                                    assetsAssigned = true;
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
                            parameters.Add("@AssetId", selection.AssetId, DbType.String);
                            try
                            {
                                db.Query<GroupedAssignments, ErrorAssignment, ErrorAssignment>
                                (sql, (group, assignment) =>
                                {
                                    int groupIndex = GetGroupIndex(group.Id, groupDictionary, groupedAssignments, group);
                                    assignment.Message = errorMessage;
                                    groupedAssignments[groupIndex].Assignments.Add(assignment);
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
                UserSummary summary = new UserSummary
                {
                    Id = postModel.UserId,
                    Name = postModel.UserDetails.Name,
                    Created = created,
                    ActiveAssignments = assetsAssigned,
                    GroupedAssignments = groupedAssignments
                };
                return summary;
            }
        }

        //++++++++++++++++++
        //--HELPER METHODS--
        //++++++++++++++++++

        private Boolean AddNewAssignment(IDbConnection db, IDbTransaction transaction, Dictionary<int, int> groupDictionary, List<GroupedAssignments> groupedAssignments, NewSelection selection, int userId)
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

            var parameters = new DynamicParameters(selection);
            parameters.Add("@UserId", userId, DbType.Int32);
            try
            {
                db.Query<GroupedAssignments, QualifiedAssignment, QualifiedAssignment>
                (sql, (group, assignment) =>
                {
                    int groupIndex = GetGroupIndex(selection.GroupId, groupDictionary, groupedAssignments, group);
                    assignment.Action = ActionType.Assign;
                    groupedAssignments[groupIndex].Assignments.Add(assignment);
                    return null;
                }, parameters, transaction, splitOn: "Model");
                return true;
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
                parameters.Add("@ModelId", selection.ModelId, DbType.Int32);
                parameters.Add("@GroupId", selection.GroupId, DbType.Int32);
                try
                {
                    db.Query<GroupedAssignments, ErrorAssignment, ErrorAssignment>
                    (sql, (group, assignment) =>
                    {
                        int groupIndex = GetGroupIndex(selection.GroupId, groupDictionary, groupedAssignments, group);
                        assignment.Message = "Unable to assign Asset";
                        groupedAssignments[groupIndex].Assignments.Add(assignment);
                        return null;
                    }, parameters, transaction, splitOn: "Model");
                    return false;
                }
                catch
                {
                    throw;
                }
            }
        }

        private int GetGroupIndex(int groupId, Dictionary<int, int> groupDictionary, List<GroupedAssignments> groupedAssignments, GroupedAssignments group)
        {
            if (!groupDictionary.TryGetValue(groupId, out int groupIndex))
            {
                groupIndex = groupedAssignments.Count();
                groupDictionary.Add(groupId, groupIndex);
                group.Assignments = new List<Assignment>();
                groupedAssignments.Add(group);
            }
            return groupIndex;
        }

        private string CheckStatus(DateTime dueDate)
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