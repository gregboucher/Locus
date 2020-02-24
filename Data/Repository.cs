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
                        FROM [dbo].[Assignment] AS Asg
                             INNER JOIN [dbo].[User] AS U
                                ON Asg.UserId = U.Id
	                               INNER JOIN [dbo].[Role] AS R
                                      ON U.RoleId = R.Id
                             INNER JOIN [dbo].[Asset] AS Ast
                                ON Asg.AssetId = Ast.Id
                                   INNER JOIN [dbo].[Group] AS G
                                      ON Ast.GroupId = G.Id
	                               INNER JOIN [dbo].[Model] AS M
                                      ON Ast.ModelId = M.Id
                                         INNER JOIN [dbo].[Icon] AS I
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
                        asset.Status = AssetStatus(asset.Due.Date);
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
            string sql =
                    @"SELECT COUNT(*)
                        FROM [dbo].[Assignment] AS Asg 
                             INNER JOIN [dbo].[Asset] AS Ast
	                            ON Asg.AssetId = Ast.Id
	                               INNER JOIN [dbo].[Model] AS M
		                              ON Ast.ModelId = M.Id
                       WHERE Asg.Returned IS NULL 
                         AND cast(GETDATE() as date) = cast(Asg.Due as date);";
            return GetScalar(sql);
        }

        public int CountOverdue()
        {
            string sql =
                     @"SELECT COUNT(*)
                        FROM [dbo].[Assignment] AS Asg 
                             INNER JOIN [dbo].[Asset] AS Ast
	                            ON Asg.AssetId = Ast.Id
	                               INNER JOIN [dbo].[Model] AS M
		                              ON Ast.ModelId = M.Id
                       WHERE Asg.Returned IS NULL
                         AND cast(GETDATE() as date) > cast(Asg.Due as date);";
            return GetScalar(sql);
        }

        public int CountUsersCreatedToday()
        {
            string sql =
                     @"SELECT COUNT(DISTINCT Asg.UserId)
                        FROM [dbo].[Assignment] As Asg
                       WHERE Asg.Returned IS NULL
                         AND cast(GETDATE() as date) = cast(Asg.Assigned as date);";
            return GetScalar(sql);
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
                        FROM [dbo].[Asset] AS Ast
                             LEFT JOIN [dbo].[Assignment] AS Asg
                               ON Ast.Id = Asg.AssetId
                              AND Asg.Returned IS NULL
                             RIGHT JOIN (
	                               SELECT Ast.GroupId,
                                          Ast.ModelId,
                                          MAX(CASE WHEN @userId IS NOT NULL AND Asg.UserId = @userId THEN Asg.UserId ELSE NULL END) AS [User],
                                          COUNT(*) AS Total,
                                          SUM(CASE WHEN Asg.UserId IS NULL OR Asg.Returned IS NOT NULL THEN 1 ELSE 0 END) AS Surplus
		                             FROM [dbo].[Asset] AS Ast
		                                  LEFT JOIN [dbo].[Assignment] AS Asg
		                                    ON Asg.AssetId = Ast.Id
                                           AND Asg.Returned IS NULL
	                                GROUP BY Ast.GroupId, Ast.ModelId
	                         ) AS Grouped
	                         ON Grouped.[User] = Asg.UserId
                            AND Grouped.GroupId = Ast.GroupId
                            AND Grouped.ModelId = Ast.ModelId
	                            LEFT JOIN [dbo].[Group] AS G
                                  ON G.Id = Grouped.GroupId
	                            LEFT JOIN [dbo].[Model] As M
                                  ON M.Id = Grouped.ModelId
	                                 INNER JOIN [dbo].[Icon] AS I
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
                            asset.Status = AssetStatus(asset.Due.Date);
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
                                 FROM [dbo].[Role] AS R
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
                    throw new LocusException("Unable to populate list of User Roles.");
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
                                 FROM [dbo].[User] AS U
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

        public UserSummary CreateNewUser(UserCreatePostModel postModel)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {        
                string sql = @"INSERT INTO [dbo].[User]
                               VALUES (@Name,
                                       @Absentee,
                                       @Email,
                                       @Phone,
                                       GETDATE(),
                                       @Comment,
                                       @RoleId)
                                SELECT @UserId = SCOPE_IDENTITY(),
                                       @Created = Created
							      FROM [dbo].[User]
								 WHERE Id = SCOPE_IDENTITY();";
                
                var parameters = new DynamicParameters(postModel.UserDetails);
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
                    var userStatus = Status.Inactive;
                    foreach (var selection in postModel.AssignSelections)
                    {
                        if (AddNewAssignment(db, transaction, groupDictionary, groupedAssignments, selection, userId))
                            userStatus = Status.Active;
                    }
                    if (groupedAssignments.Any()) {
                        transaction.Commit();
                        var summary = new UserSummary
                        {
                            Id = userId,
                            Name = postModel.UserDetails.Name,
                            Created = parameters.Get<DateTime>("@Created"),
                            Status = userStatus,
                            GroupedAssignments = groupedAssignments
                        };
                        return summary;
                    }
                    var exception = new LocusException(
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
                string sql = @"UPDATE [dbo].[User]
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
                var created = new DateTime();
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
                var userStatus = Status.Inactive;
                if (postModel.AssignSelections != null)
                {
                    foreach (var selection in postModel.AssignSelections)
                    {
                        if (AddNewAssignment(db, null, groupDictionary, groupedAssignments, selection, postModel.UserId))
                            userStatus = Status.Active;
                    }
                }
                if (postModel.EditSelections != null)
                {
                    //UPDATE will not throw and exception by default if it can't set.
                    //Most likely cause would be if the asset has already been returned.
                    //If @AssignmentId is null, the appended select query will return no rows
                    //and an Assignment object will be created with null members.
                    //hence we bypass this by manually throwing an exception if @AssignmentId is null.
                    string sqlReturn = @"UPDATE [dbo].[Assignment]
                                            SET Returned = GETDATE(),
                                                @AssignmentId = Id
                                          WHERE AssetId = @AssetId
                                            AND Returned IS NULL
                                             IF (@AssignmentId IS NULL)
                                          THROW 50001,
                                                'AssignmentId is null',
                                                1;";

                    string sqlExtend = @"UPDATE Asg
                                            SET Due = DATEADD(DAY, M.[Period], GETDATE()),
                                                @AssignmentId = Asg.Id
                                           FROM [dbo].[Assignment] AS Asg
	                                            INNER JOIN [dbo].[Asset] AS Ast
	                                               ON Ast.Id = Asg.AssetId
	                                                  INNER JOIN [dbo].[Model] AS M
		                                                 ON M.Id = Ast.ModelId
                                          WHERE Asg.AssetId = @AssetId
                                            AND Asg.Returned IS NULL
                                             IF (@AssignmentId IS NULL)
                                          THROW 50001,
                                                'AssignmentId is null',
                                                1;";

                    int count = postModel.EditSelections.Count;
                    for (int i = 0; i < count; i++)
                    {
                        EditSelection selection = postModel.EditSelections[i];
                        string errorMessage = "";
                        switch (selection.Type)
                        {
                            case SelectionType.Return:
                                sql = sqlReturn;
                                errorMessage = @"Unable to return this Asset
                                                 ";
                                break;
                            case SelectionType.Extend:
                                sql = sqlExtend;
                                errorMessage = @"Unable to extend this Assignment";
                                break;
                        }

                        sql += Environment.NewLine + @"SELECT G.Id,
                                                              G.[Name],
                                                              M.[Name] AS Model,
                                                              I.[Name] AS Icon,
                                                              Ast.Tag,
                                                              Asg.Due
                                                         FROM [dbo].[Assignment] AS Asg
                                                              INNER JOIN [dbo].[Asset] AS Ast
                                                                 ON Ast.Id = Asg.AssetId
                                                                    INNER JOIN [dbo].[Group] AS G
                                                                       ON G.Id = Ast.GroupId
                                                                    INNER JOIN [dbo].[Model] AS M
                                                                       ON M.Id = Ast.ModelId
                                                                          INNER JOIN [dbo].[Icon] as I
                                                                             ON I.Id = M.IconId
                                                        WHERE Asg.Id = @AssignmentId;";

                        parameters = new DynamicParameters();
                        parameters.Add("@AssetId", selection.AssetId, DbType.String);
                        parameters.Add("@AssignmentId", -1, DbType.Int32, ParameterDirection.Output);
                        try
                        {
                            switch (selection.Type)
                            {
                                case SelectionType.Return:
                                    UpdateAssignment<ReturnedAssignment>("Model", null, parameters, sql, db, null, groupDictionary, groupedAssignments);
                                    break;
                                case SelectionType.Extend:
                                    if (UpdateAssignment<ExtendAssignment>("Model", null, parameters, sql, db, null, groupDictionary, groupedAssignments))
                                        userStatus = Status.Active;
                                    break;
                            }
                        }
                        catch
                        {
                            sql = @"SELECT G.Id,
	                                       G.[Name],
                                           M.[Name] AS Model,
                                           I.[Name] AS Icon,
                                           Ast.Tag
                                      FROM [dbo].[Asset] AS Ast
	                                       INNER JOIN [dbo].[Group] AS G
	                                          ON G.Id = Ast.GroupId
                                           INNER JOIN [dbo].[Model] AS M
                                              ON M.Id = Ast.ModelId
		                                         INNER JOIN [dbo].[Icon] AS I
		                                            ON I.Id = M.IconId
                                     WHERE Ast.Id = @AssetId";

                            parameters = new DynamicParameters();
                            parameters.Add("@AssetId", selection.AssetId, DbType.String);
                            try
                            {
                                UpdateAssignment<ErrorAssignment>("Model", errorMessage, parameters, sql, db, null, groupDictionary, groupedAssignments);
                            }
                            catch (Exception ex)
                            {
                                _logger.WriteLog(ex);
                                throw;
                            }
                        }
                        //on last iteration, query db for count of user assignments,
                        //if > 0, return Status.active
                        if (i == count - 1 && userStatus != Status.Active)
                        {
                            userStatus = UserStatus(postModel.UserId, db, null);
                        }
                    }
                }
                var summary = new UserSummary
                {
                    Id = postModel.UserId,
                    Name = postModel.UserDetails.Name,
                    Created = created,
                    Status = userStatus,
                    GroupedAssignments = groupedAssignments
                };
                return summary;
            }
        }

        //++++++++++++++++++
        //--HELPER METHODS--
        //++++++++++++++++++

        private Boolean AddNewAssignment(IDbConnection db, IDbTransaction transaction, Dictionary<int, int> groupDictionary, List<GroupedAssignments> groupedAssignments, AssignSelection selection, int userId)
        {
            string sql = @"INSERT INTO [dbo].[Assignment]
                            SELECT GETDATE(),
	                               DATEADD(DAY, M.[Period], GETDATE()),
	                               NULL,
	                               @UserId,
	                               (
		                             SELECT TOP 1 Ast.Id
                                       FROM [dbo].[Asset] AS Ast
	                                        LEFT JOIN [dbo].[Assignment] AS Asg
                                              ON Ast.Id = Asg.AssetId
                                      WHERE (Asg.AssetId NOT IN
                                            (
	                                           SELECT AssetId
		                                         FROM [dbo].[Assignment]
		                                        WHERE Returned IS NULL
	                                        )
	                                     OR Asg.AssetId IS NULL)
                                        AND Ast.Deactivated IS NULL
                                        AND Ast.GroupId = @GroupId
                                        AND Ast.ModelId = @ModelId
                                      ORDER BY NEWID()
	                               )
                              FROM [dbo].[Model] AS M
                             WHERE M.Id = @ModelId

                            SELECT G.Id,
                                   G.[Name],
                                   M.[Name] AS Model,
                                   I.[Name] AS Icon,
                                   Ast.Tag,
                                   Asg.Due
							  FROM [dbo].[Assignment] AS Asg
							       INNER JOIN [dbo].[Asset] AS Ast
							          ON Ast.Id = Asg.AssetId
                                         INNER JOIN [dbo].[Group] AS G
									        ON G.Id = Ast.GroupId
								         INNER JOIN [dbo].[Model] AS M
								            ON M.Id = Ast.ModelId
                                               INNER JOIN [dbo].[Icon] as I
                                                  ON I.Id = M.IconId
							 WHERE Asg.Id = SCOPE_IDENTITY();";

            var parameters = new DynamicParameters(selection);
            parameters.Add("@UserId", userId, DbType.Int32);
            try
            {
                return UpdateAssignment<NewAssignment>("Model", null, parameters, sql, db, transaction, groupDictionary, groupedAssignments);
            }
            catch
            {
                sql = @"SELECT G.Id,
                               G.[Name],
                               M.[Name] AS Model,
                               I.[Name] AS Icon
                          FROM [dbo].[Model] AS M
                               INNER JOIN [dbo].[Icon] AS I
                                  ON I.Id = M.IconId
                               CROSS JOIN [dbo].[Group] AS G
                         WHERE M.Id = @ModelId
                           AND G.Id = @GroupId;";

                parameters = new DynamicParameters();
                parameters.Add("@ModelId", selection.ModelId, DbType.Int32);
                parameters.Add("@GroupId", selection.GroupId, DbType.Int32);
                try
                {
                    UpdateAssignment<ErrorAssignment>("Model", "Unable to assign Asset", parameters, sql, db, transaction, groupDictionary, groupedAssignments);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw;
                }
            }
        }

        private Boolean UpdateAssignment<T>(string splitString, string errorMessage, DynamicParameters parameters, string sql, IDbConnection db, IDbTransaction transaction, Dictionary<int, int> groupDictionary, List<GroupedAssignments> groupedAssignments) where T : Assignment, new()
        {
            db.Query<GroupedAssignments, T, T>
            (sql, (group, assignment) =>
            {
                if (!groupDictionary.TryGetValue(group.Id, out int groupIndex))
                {
                    groupIndex = groupedAssignments.Count();
                    groupDictionary.Add(group.Id, groupIndex);
                    group.Assignments = new List<Assignment>();
                    groupedAssignments.Add(group);
                }
                if (assignment is ErrorAssignment)
                    (assignment as ErrorAssignment).Message = errorMessage;
                groupedAssignments[groupIndex].Assignments.Add(assignment);
                return null;
            }, parameters, transaction, splitOn: splitString);
            return true;
        }

        public Status UserStatus(int userId, IDbConnection db, IDbTransaction transaction)
        {
            if (db == null)
                db = _connectionFactory.GetConnection();

            using (db)
            {
                string sql = @"SELECT COUNT(*)
					         FROM [dbo].[Assignment]
						    WHERE UserId = @UserId
						      AND Returned IS NULL;";

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId, DbType.Int32);
                try
                {
                    int assetCount = db.ExecuteScalar<int>(sql, parameters, transaction);
                    if (assetCount > 0)
                    {
                        return Status.Active;
                    }
                    else if (assetCount == 0)
                    {
                        return Status.Inactive;
                    }
                    else return Status.Error;
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    return Status.Error;
                }
            }
        }

        private Status AssetStatus(DateTime dueDate)
        {
            if (DateTime.Now.Date < dueDate)
            {
                return Status.Active;
            }
            else if (DateTime.Now.Date == dueDate)
            {
                return Status.Due;
            }
            else return Status.Overdue;
        }

        private int GetScalar(string sql)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
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
    }
}