using System.Linq;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System;
using Locus.ViewModels;
using Locus.Models;
using System.Transactions;

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

        public IEnumerable<CollectionOfGenerics<User>> GetUsersByCollection()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT U.Id,
                             U.[Name],
                             U.Created,
                             R.[Name] AS Role,
                             Ast.Id,
                             Ast.Tag,
                             I.[Name] AS Icon,
                             Asg.Assigned,
                             Asg.Due,
                             C.Id,
                             C.[Name]
                        FROM [dbo].[Assignment] AS Asg
                             INNER JOIN [dbo].[User] AS U
                                ON Asg.UserId = U.Id
	                               INNER JOIN [dbo].[Role] AS R
                                      ON U.RoleId = R.Id
                             INNER JOIN [dbo].[Asset] AS Ast
                                ON Asg.AssetId = Ast.Id
                                   INNER JOIN [dbo].[Collection] AS C
                                      ON Ast.CollectionId = C.Id
	                               INNER JOIN [dbo].[Model] AS M
                                      ON Ast.ModelId = M.Id
                                         INNER JOIN [dbo].[Icon] AS I
                                            ON M.IconId = I.Id
                       WHERE Asg.Returned IS NULL
                       ORDER BY C.[Name], U.Id, Asg.Due;";

                var collectionsOfUsers = new List<CollectionOfGenerics<User>>();
                var collectionDictionary = new Dictionary<int, int>();
                var userDictionary = new Dictionary<Tuple<int, int>, int>();
                
                try
                {
                    db.Query<User, Asset, CollectionOfGenerics<User>, User>
                    (sql, (user, asset, collection) =>
                    {
                        asset.Status = AssetStatus(asset.Due.Date);
                        if (!collectionDictionary.TryGetValue(collection.Id, out int collectionIndex))
                        {
                            collectionIndex = collectionsOfUsers.Count();
                            collectionDictionary.Add(collection.Id, collectionIndex);
                            collection.TList = new List<User>();
                            collectionsOfUsers.Add(collection);
                        }
                        var key = new Tuple<int, int>(collection.Id, user.Id);
                        if (!userDictionary.TryGetValue(key, out int userIndex))
                        {
                            userIndex = collectionsOfUsers[collectionIndex].TList.Count();
                            userDictionary.Add(key, userIndex);
                            user.Assets = new List<Asset>();
                            collectionsOfUsers[collectionIndex].TList.Add(user);
                        }
                        collectionsOfUsers[collectionIndex].TList[userIndex].Assets.Add(asset);
                        return null;
                    });
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate assignment table.");
                }
                return collectionsOfUsers;
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

        public IEnumerable<CollectionOfModels<Model>> GetModelsByCollection(int? id)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql =
                    @"SELECT Grouped.CollectionId AS Id,
	                         C.[Name],
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
	                               SELECT Ast.CollectionId,
                                          Ast.ModelId,
                                          MAX(CASE WHEN @userId IS NOT NULL AND Asg.UserId = @userId THEN Asg.UserId ELSE NULL END) AS [User],
                                          COUNT(*) AS Total,
                                          SUM(CASE WHEN Asg.UserId IS NULL OR Asg.Returned IS NOT NULL THEN 1 ELSE 0 END) AS Surplus
		                             FROM [dbo].[Asset] AS Ast
		                                  LEFT JOIN [dbo].[Assignment] AS Asg
		                                    ON Asg.AssetId = Ast.Id
                                           AND Asg.Returned IS NULL
	                                GROUP BY Ast.CollectionId, Ast.ModelId
	                         ) AS Grouped
	                         ON Grouped.[User] = Asg.UserId
                            AND Grouped.CollectionId = Ast.CollectionId
                            AND Grouped.ModelId = Ast.ModelId
	                            LEFT JOIN [dbo].[Collection] AS C
                                  ON C.Id = Grouped.CollectionId
	                            LEFT JOIN [dbo].[Model] As M
                                  ON M.Id = Grouped.ModelId
	                                 INNER JOIN [dbo].[Icon] AS I
                                        ON I.Id = M.IconId
                    ORDER BY Grouped.CollectionId, Asg.Due DESC;";

                var collectionsOfModels = new List<CollectionOfModels<Model>>();
                var collectionDictionary = new Dictionary<int, int>();

                try
                {
                    db.Query<CollectionOfModels<Model>, Model, Asset, Asset>
                    (sql, (collection, model, asset) =>
                    {
                        if (!collectionDictionary.TryGetValue(collection.Id, out int collectionIndex))
                        {
                            collectionIndex = collectionsOfModels.Count();
                            collectionDictionary.Add(collection.Id, collectionIndex);
                            collection.TList = new List<Model>();
                            collectionsOfModels.Add(collection);
                        }
                        if (asset != null)
                        {
                            asset.Status = AssetStatus(asset.Due.Date);
                            model.Asset = asset;
                            ++collectionsOfModels[collectionIndex].TotalAssigned;
                        }
                        collectionsOfModels[collectionIndex].TList.Add(model);
                        collectionsOfModels[collectionIndex].Total += model.Total;
                        return null;
                    }, new { userId = id });
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate model list.");
                }
                return collectionsOfModels;
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

        public Report CreateNewUser(UserCreatePostModel postModel)
        {
            using (var scope = new TransactionScope())
            {
                var collectionsOfReportItems = new List<CollectionOfGenerics<ReportItem>>();
                var collectionDictionary = new Dictionary<int, int>();
                var userStatus = Status.Inactive;
                var parameters = new DynamicParameters(postModel.UserDetails);
                using (var db = _connectionFactory.GetConnection())
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

                    parameters.Add("@UserId", 0, DbType.Int32, ParameterDirection.Output);
                    parameters.Add("@Created", 0, DbType.DateTime, ParameterDirection.Output);
                    try
                    {
                        db.Execute(sql, parameters);
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog(ex);
                        throw new LocusException("Unable to create new user.");
                    }

                    foreach (var operation in postModel.AssignmentOperations)
                    {
                        if (CreateNewAssignment(db, collectionDictionary, collectionsOfReportItems, operation, parameters.Get<int>("@UserId")))
                            userStatus = Status.Active;
                    }
                }
                if (collectionsOfReportItems.Any())
                {
                    scope.Complete();
                    var summary = new Report
                    {
                        UserId = parameters.Get<int>("@UserId"),
                        UserName = postModel.UserDetails.Name,
                        UserCreated = parameters.Get<DateTime>("@Created"),
                        UserStatus = userStatus,
                        CollectionsOfReportItems = collectionsOfReportItems
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

        public Report EditExistingUser(UserEditPostModel postModel)
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
                var collectionsOfReportItems = new List<CollectionOfGenerics<ReportItem>>();
                var collectionDictionary = new Dictionary<int, int>();
                var userStatus = Status.Inactive;
                if (postModel.AssignmentOperations != null)
                {
                    foreach (var operation in postModel.AssignmentOperations)
                    {
                        if (CreateNewAssignment(db, collectionDictionary, collectionsOfReportItems, operation, postModel.UserId))
                            userStatus = Status.Active;
                    }
                }
                if (postModel.EditOperations != null)
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

                    //TODO @@ROWCOUNT = 0 THROW!!!!

                    int count = postModel.EditOperations.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var operation = postModel.EditOperations[i];
                        string errorMessage = "";
                        switch (operation.Type)
                        {
                            case OperationType.Return:
                                sql = sqlReturn;
                                errorMessage = "Unable to return this Asset";
                                break;
                            case OperationType.Extension:
                                sql = sqlExtend;
                                errorMessage = @"Unable to extend this Assignment";
                                break;
                        }

                        sql += Environment.NewLine + @"SELECT C.Id,
                                                              C.[Name],
                                                              M.[Name] AS Model,
                                                              M.Id AS ModelId,
                                                              I.[Name] AS Icon,
                                                              Ast.Tag,
                                                              Asg.Due
                                                         FROM [dbo].[Assignment] AS Asg
                                                              INNER JOIN [dbo].[Asset] AS Ast
                                                                 ON Ast.Id = Asg.AssetId
                                                                    INNER JOIN [dbo].[Collection] AS C
                                                                       ON C.Id = Ast.CollectionId
                                                                    INNER JOIN [dbo].[Model] AS M
                                                                       ON M.Id = Ast.ModelId
                                                                          INNER JOIN [dbo].[Icon] as I
                                                                             ON I.Id = M.IconId
                                                        WHERE Asg.Id = @AssignmentId;";

                        parameters = new DynamicParameters();
                        parameters.Add("@AssetId", operation.AssetId, DbType.String);
                        parameters.Add("@AssignmentId", -1, DbType.Int32, ParameterDirection.Output);
                        try
                        {
                            switch (operation.Type)
                            {
                                case OperationType.Return:
                                    var returnItem = DbQuery<ReturnedReportItem>(sql, "Model", null, db, parameters);
                                    CommitReportItemToSummary(returnItem, collectionDictionary, collectionsOfReportItems);
                                    break;
                                case OperationType.Extension:
                                    var extensionItem = DbQuery<ExtensionReportItem>(sql, "Model", null, db, parameters);
                                    AppendCustomeProperties(extensionItem, db);
                                    CommitReportItemToSummary(extensionItem, collectionDictionary, collectionsOfReportItems);
                                    userStatus = Status.Active;
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex);

                            sql = @"SELECT C.Id,
	                                       C.[Name],
                                           M.[Name] AS Model,
                                           I.[Name] AS Icon,
                                           Ast.Tag
                                      FROM [dbo].[Asset] AS Ast
	                                       INNER JOIN [dbo].[Collection] AS C
	                                          ON C.Id = Ast.CollectionId
                                           INNER JOIN [dbo].[Model] AS M
                                              ON M.Id = Ast.ModelId
		                                         INNER JOIN [dbo].[Icon] AS I
		                                            ON I.Id = M.IconId
                                     WHERE Ast.Id = @AssetId";

                            parameters = new DynamicParameters();
                            parameters.Add("@AssetId", operation.AssetId, DbType.String);
                            try
                            {
                                var errorItem = DbQuery<ErrorReportItem>(sql, "Model", errorMessage, db, parameters);
                                CommitReportItemToSummary(errorItem, collectionDictionary, collectionsOfReportItems);
                            }
                            catch (Exception exception)
                            {
                                _logger.WriteLog(exception);
                                throw;
                            }
                        }
                        //on last iteration, query db for count of user assignments,
                        //if > 0, return Status.active
                        if (i == count - 1 && userStatus != Status.Active)
                        {
                            userStatus = UserStatus(postModel.UserId, db);
                        }
                    }
                }
                var summary = new Report
                {
                    UserId = postModel.UserId,
                    UserName = postModel.UserDetails.Name,
                    UserCreated = created,
                    UserStatus = userStatus,
                    CollectionsOfReportItems = collectionsOfReportItems
                };
                return summary;
            }
        }

        //++++++++++++++++++
        //--HELPER METHODS--
        //++++++++++++++++++

        private Boolean CreateNewAssignment(IDbConnection db, Dictionary<int, int> collectionDictionary, List<CollectionOfGenerics<ReportItem>> collectionsOfReportItems, AssignmentOperation operation, int userId)
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
                                        AND Ast.CollectionId = @CollectionId
                                        AND Ast.ModelId = @ModelId
                                      ORDER BY NEWID()
	                               )
                              FROM [dbo].[Model] AS M
                             WHERE M.Id = @ModelId

                            SELECT C.Id,
                                   C.[Name],
                                   M.[Name] AS Model,
                                   M.Id AS ModelId,
                                   I.[Name] AS Icon,
                                   Ast.Tag,
                                   Asg.Due
							  FROM [dbo].[Assignment] AS Asg
							       INNER JOIN [dbo].[Asset] AS Ast
							          ON Ast.Id = Asg.AssetId
                                         INNER JOIN [dbo].[Collection] AS C
									        ON C.Id = Ast.CollectionId
								         INNER JOIN [dbo].[Model] AS M
								            ON M.Id = Ast.ModelId
                                               INNER JOIN [dbo].[Icon] as I
                                                  ON I.Id = M.IconId
							 WHERE Asg.Id = SCOPE_IDENTITY();";

            var parameters = new DynamicParameters(operation);
            parameters.Add("@UserId", userId, DbType.Int32);
            try
            {
                var assignmentItem = DbQuery<AssignmentReportItem>(sql, "Model", null, db, parameters);
                AppendCustomeProperties(assignmentItem, db);
                CommitReportItemToSummary(assignmentItem, collectionDictionary, collectionsOfReportItems);
                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                
                sql = @"SELECT C.Id,
                               C.[Name],
                               M.[Name] AS Model,
                               I.[Name] AS Icon
                          FROM [dbo].[Model] AS M
                               INNER JOIN [dbo].[Icon] AS I
                                  ON I.Id = M.IconId
                               CROSS JOIN [dbo].[Collection] AS C
                         WHERE M.Id = @ModelId
                           AND C.Id = @CollectionId;";

                parameters = new DynamicParameters();
                parameters.Add("@ModelId", operation.ModelId, DbType.Int32);
                parameters.Add("@CollectionId", operation.CollectionId, DbType.Int32);
                try
                {
                    var errorItem = DbQuery<ErrorReportItem>(sql, "Model", "Unable to Assign asset", db, parameters);
                    CommitReportItemToSummary(errorItem, collectionDictionary, collectionsOfReportItems);
                    return false;
                }
                catch (Exception exception)
                {
                    _logger.WriteLog(exception);
                    throw;
                }
            }
        }

        private PendingReportItem<T> DbQuery<T>(string sql, string splitString, string errorMessage, IDbConnection db, DynamicParameters parameters)
            where T : ReportItem
        {
            var item = db.Query<Collection, T, PendingReportItem<T>>
            (sql, (collection, reportItem) =>
            {
                if (typeof(T) == typeof(ErrorReportItem))
                    (reportItem as ErrorReportItem).Message = errorMessage;
                var newItem = new PendingReportItem<T>
                {
                    Collection = collection,
                    ReportItem = reportItem
                };
                return newItem;
            }, parameters, splitOn: splitString).Single();
            return item;
        }

        private void AppendCustomeProperties<T>(PendingReportItem<T> item, IDbConnection db)
            where T : DetailedReportItem
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ModelId", item.ReportItem.ModelId, DbType.Int32);
            
            string sql = @"SELECT [Name],
                                  QueryString AS [Value]
                             FROM [dbo].[Query]
                            WHERE ModelId = @ModelId";
            
            var queries = db.Query<CustomProperty>(sql, parameters);
            if (queries.Any())
            {
                item.ReportItem.CustomProperties = new List<CustomProperty>();
                foreach (var query in queries)
                {
                    var property = new CustomProperty
                    {
                        Name = query.Name,
                        Value = db.ExecuteScalar<string>(query.Value)
                    };
                    item.ReportItem.CustomProperties.Add(property);
                }
            }
        }     

        private void CommitReportItemToSummary<T>(PendingReportItem<T> item, Dictionary<int, int> collectionDictionary, List<CollectionOfGenerics<ReportItem>> collectionsOfReportItems)
            where T : ReportItem
        {
            if (!collectionDictionary.TryGetValue(item.Collection.Id, out int collectionIndex))
            {
                collectionIndex = collectionsOfReportItems.Count();
                collectionDictionary.Add(item.Collection.Id, collectionIndex);
                var collection = new CollectionOfGenerics<ReportItem>
                {
                    Id = item.Collection.Id,
                    Name = item.Collection.Name,
                    TList = new List<ReportItem>()
                };
                collectionsOfReportItems.Add(collection);
            }
            collectionsOfReportItems[collectionIndex].TList.Add(item.ReportItem);
        }

        public Status UserStatus(int userId, IDbConnection db)
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
                    int assetCount = db.ExecuteScalar<int>(sql, parameters);
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