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

        public IEnumerable<ListTByCollection<User>> GetUsersByCollection()
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
                             M.[Name] AS Model,
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
                       ORDER BY C.[Name], U.Id, CASE WHEN Asg.Due IS NULL THEN 1 ELSE 0 END;";
                
                var collectionsOfUsers = new List<ListTByCollection<User>>();
                var collectionDictionary = new Dictionary<int, int>();
                var userDictionary = new Dictionary<Tuple<int, int>, int>();
                
                try
                {
                    db.Query<User, Asset, ListTByCollection<User>, User>
                    (sql, (user, asset, collection) =>
                    {
                        asset.Status = AssetStatus(asset.Due);
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
                     @"SELECT COUNT(DISTINCT UserId)
                        FROM [dbo].[Assignment]
                       WHERE Returned IS NULL
                         AND cast(GETDATE() as date) = cast(Assigned as date);";
            return GetScalar(sql);
        }

        public int CountIndefinite()
        {
            string sql =
                     @"SELECT COUNT(*)
                        FROM [dbo].[Assignment]
                       WHERE Returned IS NULL
                         AND Due IS NULL;";
            return GetScalar(sql);
        }

        public IEnumerable<ListModelsByCollection<Model>> GetModelsByCollection(int? id)
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

                var collectionsOfModels = new List<ListModelsByCollection<Model>>();
                var collectionDictionary = new Dictionary<int, int>();

                try
                {
                    db.Query<ListModelsByCollection<Model>, Model, Asset, Asset>
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
                            asset.Status = AssetStatus(asset.Due);
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
                    throw new InvalidOperationException();
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
            var results = new List<Result>();
            int userId;
            using (var scope = new TransactionScope())
            {
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
                    try
                    {
                        db.Execute(sql, parameters);
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog(ex);
                        throw new LocusException("Unable to create new user.");
                    }
                    userId = parameters.Get<int>("@UserId");
                    foreach (var operation in postModel.AssignmentOperations)
                    {
                        CreateNewAssignment(db, results, operation, userId);
                    }
                }
                if (results.Any())
                {
                    scope.Complete();
                }
                else
                {
                    var exception = new LocusException(
                    @"Unfortunately, we were unable to assign any assets to this user.
                      The asset pool for the selected model(s) may have since been depleted.");
                    _logger.WriteLog(exception);
                    throw exception;
                }
            }
            GenerateReport(results, userId);
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
                var collectionsOfReportItems = new List<ListTByCollection<ReportItem>>();
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
                                                              Asg.Due,
                                                              Asg.Id AS AssignmentId
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
                                    var returnItem = DbQuery<ReturnedReportItem>(sql, null, db, parameters);
                                    CommitReportItemToSummary(returnItem, collectionDictionary, collectionsOfReportItems);
                                    break;
                                case OperationType.Extension:
                                    var extensionItem = DbQuery<ExtensionReportItem>(sql, null, db, parameters);
                                    AppendCustomeProperties(extensionItem, db);
                                    CommitReportItemToSummary(extensionItem, collectionDictionary, collectionsOfReportItems);
                                    userStatus = Status.Active;
                                    break;
                                default:
                                    throw new InvalidOperationException();
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
                                var errorItem = DbQuery<ErrorReportItem>(sql, errorMessage, db, parameters);
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

        private void CreateNewAssignment(IDbConnection db, IList<Result> results, AssignmentOperation operation, int userId)
        {
            //assignments with Due IS NULL are treated as indefinite
            string dueDate = "DATEADD(DAY, M.[Period], GETDATE())";
            if (operation.Type == OperationType.Indefinite_Assignment)
                dueDate = "null";

            string sql = @"INSERT INTO [dbo].[Assignment]
                            SELECT GETDATE(),
	                               " + dueDate + @",
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
                            SELECT @AssignmentId = SCOPE_IDENTITY();";

            var parameters = new DynamicParameters(operation);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@AssignmentId", -1, DbType.Int32, ParameterDirection.Output);
            results.Add(DbQuery(operation, sql, db, parameters));
        }

        private Result DbQuery(Operation operation, string sql, IDbConnection db, DynamicParameters parameters)
        {
            try
            {
                db.Execute(sql, parameters);
                return new PositiveResult(parameters.Get<int>("@AssignmentId"), operation);
            }
            catch
            {
                if (operation is AssignmentOperation)
                {
                    return new NegativeAssignmentResult(operation as AssignmentOperation);
                }
                else if (operation is EditOperation)
                {
                    return new NegativeEditResult(operation as EditOperation);
                }
                return null;
            }
        }

        public Report GenerateReport(IList<Result> results, int userId)
        {
            var listOfPartials = new List<IPartialReportItem<IReportItem>>();
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string sql = "";
                if (results == null)
                {
                    sql = @"SELECT C.Id,
	                               C.[Name],
	                               M.[Name] AS Model,
	                               I.[Name] AS Icon,
	                               Ast.Tag,
	                               Asg.Due,
	                               Asg.Id AS AssignmentId
                              FROM [dbo].[Assignment] AS Asg
	                               INNER JOIN [dbo].[Asset] AS Ast
		                              ON Ast.Id = Asg.AssetId
                                         INNER JOIN [dbo].[Collection] AS C
			                                ON C.Id = Ast.CollectionId
			                             INNER JOIN [dbo].[Model] AS M
			                                ON M.Id = Ast.ModelId
                                               INNER JOIN [dbo].[Icon] as I
                                                  ON I.Id = M.IconId
                             WHERE Asg.UserId = @UserId
                               AND Asg.Returned IS NULL";

                    var parameters = new DynamicParameters(userId);
                    try
                    {
                        listOfPartials.AddRange(CreatePartialReportItem<DetailedReportItem>(OperationType.None, sql, db, parameters));
                    }
                    catch
                    {

                    }
                } 
                else
                {
                    foreach (var result in results)
                    {
                        var parameters = new DynamicParameters(result);
                        switch (result)
                        {
                            case PositiveResult r1:
                                sql = @"SELECT C.Id,
                                           C.[Name],
                                           M.[Name] AS Model,
                                           M.Id AS ModelId,
                                           I.[Name] AS Icon,
                                           Ast.Tag,
                                           Asg.Due,
                                           Asg.Id AS AssignmentId
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
                                break;
                            case NegativeAssignmentResult r1:
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
                                break;
                            case NegativeEditResult r1:
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
                                break;
                        }
                        try
                        {
                            if (!(result is PositiveResult))
                            {
                                var temper = CreatePartialReportItem<ErrorReportItem>(result.Type, sql, db, parameters);
                                listOfPartials.AddRange(CreatePartialReportItem<ErrorReportItem>(result.Type, sql, db, parameters));
                            }
                            else if (result.Type == OperationType.Return)
                            {
                                listOfPartials.AddRange(CreatePartialReportItem<SimpleReportItem>(result.Type, sql, db, parameters));
                            }
                            else
                            {
                                listOfPartials.AddRange(CreatePartialReportItem<DetailedReportItem>(result.Type, sql, db, parameters));
                            }
                        }
                        catch
                        {

                        }
                    }
                }
                //var listOfReportItems = new List<ListTByCollection<IReportItem>>();
                //var collectionDictionary = new Dictionary<int, int>();


                foreach (var item in listOfPartials)
                {
                    if (item.ReportItem is DetailedReportItem)
                    {

                    }
                }
                



            }
        }

        private IEnumerable<IPartialReportItem<T>> CreatePartialReportItem<T>(OperationType type, string sql, IDbConnection db, DynamicParameters parameters)
            where T : SimpleReportItem
        {
            var item = db.Query<Collection, T, IPartialReportItem<T>>
            (sql, (collection, reportItem) =>
            {
                reportItem.Type = type;
                if (typeof(T) == typeof(ErrorReportItem))
                    (reportItem as ErrorReportItem).Message = "Error attempting to perform " + type.ToString().Replace(" ", "_");
                var newItem = new PartialReportItem<T>
                {
                    Collection = collection,
                    ReportItem = reportItem
                };
                return newItem;
            }, parameters, splitOn: "Model");
            return item;
        }

        private void AppendCustomeProperties(List<PartialReportItem<DetailedReportItem>> listOfIntermediates, IDbConnection db)
        {
            foreach (var item in listOfIntermediates)
            {
                //fetch all the query strings that relate to this model
                var parameters = new DynamicParameters();
                parameters.Add("@ModelId", item.ReportItem.ModelId, DbType.Int32);

                string sql = @"SELECT [Name],
                                  QueryString AS [Value]
                             FROM [dbo].[Query]
                            WHERE ModelId = @ModelId";

                var queries = db.Query<CustomProperty>(sql, parameters);
                if (queries.Any())
                {
                    //create dynamic parameters for all potential values that a query may need
                    parameters.Add("@AssignmentId", item.ReportItem.AssignmentId, DbType.Int32);

                    sql = @"SELECT Asg.Id AS AssignmentId,
	                           Asg.Assigned AS AssignmentAssigned,
	                           Asg.Due AS AssignmentDue,
	                           Asg.Returned AS AssignmentReturned,
	                           Ast.Id AS AssetId,
	                           Ast.Tag AS AssetTag,
	                           Ast.Deactivated AS AssetDeactivated,
	                           C.Id AS CollectionId,
	                           C.[Name] AS CollectionName,
	                           C.[Description] AS CollectionDescription,
	                           C.Deactivated AS CollectionDeactivated,
	                           M.Id AS ModelId,
	                           M.[Name] AS ModelName,
	                           M.[Period] AS ModelPeriod,
	                           M.Deactivated AS ModelDeactivated,
	                           I.Id AS IconId,
	                           I.[Name] AS IconName,
	                           U.Id AS UserId,
	                           U.[Name] AS UserName,
	                           U.Created AS UserCreated,
	                           U.Absentee AS UserAbsentee,
	                           U.Phone AS UserPhone,
	                           U.Email AS UserEmail,
	                           U.Comment AS UserComment,
	                           R.Id AS RoleId,
	                           R.[Name] AS RoleName
                          FROM [dbo].[Assignment] AS Asg
                               INNER JOIN [dbo].[Asset] AS Ast
                                  ON Ast.Id = Asg.AssetId
                                     INNER JOIN [dbo].[Collection] AS C
                                        ON C.Id = Ast.CollectionId
                                     INNER JOIN [dbo].[Model] AS M
                                        ON M.Id = Ast.ModelId
                                           INNER JOIN [dbo].[Icon] as I
                                              ON I.Id = M.IconId
	                           INNER JOIN [dbo].[User] as U
	                              ON U.Id = Asg.UserId
		                             INNER JOIN [dbo].[Role] AS R
		                                ON R.Id = U.RoleId
                         WHERE Asg.Id = @AssignmentId;";

                    parameters = new DynamicParameters(db.Query(sql, parameters).Cast<IDictionary<string, object>>().ElementAt(0));
                    item.ReportItem.CustomProperties = new List<CustomProperty>();
                    foreach (var query in queries)
                    {
                        var property = new CustomProperty
                        {
                            Name = query.Name,
                            Value = db.ExecuteScalar<string>(query.Value, parameters)
                        };
                        item.ReportItem.CustomProperties.Add(property);
                    }
                }
            }
            
        }     

        private void CommitReportElement<T>(PartialReportItem<T> item, Dictionary<int, int> collectionDictionary, List<ListTByCollection<ReportItem>> listOfReportItems)
            where T : IReportItem
        {
            if (!collectionDictionary.TryGetValue(item.Collection.Id, out int collectionIndex))
            {
                collectionIndex = listOfReportItems.Count();
                collectionDictionary.Add(item.Collection.Id, collectionIndex);
                var collection = new ListTByCollection<ReportItem>
                {
                    Id = item.Collection.Id,
                    Name = item.Collection.Name,
                    TList = new List<ReportItem>()
                };
                listOfReportItems.Add(collection);
            }
            listOfReportItems[collectionIndex].TList.Add(item.ReportItem);
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

        private Status AssetStatus(DateTime? dueDate)
        {
            if (dueDate.HasValue)
            {
                if (DateTime.Now.Date < dueDate.Value.Date)
                {
                    return Status.Active;
                }
                else if (DateTime.Now.Date == dueDate.Value.Date)
                {
                    return Status.Due;
                }
                else return Status.Overdue;
            }
            return Status.Indefinite;
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