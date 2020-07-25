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
                string query =
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
                    (query, (user, asset, collection) =>
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
            string query =
                    @"SELECT COUNT(*)
                        FROM [dbo].[Assignment] AS Asg 
                             INNER JOIN [dbo].[Asset] AS Ast
	                            ON Asg.AssetId = Ast.Id
	                               INNER JOIN [dbo].[Model] AS M
		                              ON Ast.ModelId = M.Id
                       WHERE Asg.Returned IS NULL 
                         AND cast(GETDATE() as date) = cast(Asg.Due as date);";
            return GetScalar(query);
        }

        public int CountOverdue()
        {
            string query =
                     @"SELECT COUNT(*)
                        FROM [dbo].[Assignment] AS Asg 
                             INNER JOIN [dbo].[Asset] AS Ast
	                            ON Asg.AssetId = Ast.Id
	                               INNER JOIN [dbo].[Model] AS M
		                              ON Ast.ModelId = M.Id
                       WHERE Asg.Returned IS NULL
                         AND cast(GETDATE() as date) > cast(Asg.Due as date);";
            return GetScalar(query);
        }

        public int CountUsersCreatedToday()
        {
            string query =
                     @"SELECT COUNT(DISTINCT UserId)
                        FROM [dbo].[Assignment]
                       WHERE Returned IS NULL
                         AND cast(GETDATE() as date) = cast(Assigned as date);";
            return GetScalar(query);
        }

        public int CountLongterm()
        {
            string query =
                     @"SELECT COUNT(*)
                        FROM [dbo].[Assignment]
                       WHERE Returned IS NULL
                         AND Due IS NULL;";
            return GetScalar(query);
        }

        public IEnumerable<ListModelsByCollection<Model>> GetModelsByCollection(int? id)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string query =
                    @"SELECT Grouped.CollectionId AS Id,
	                         C.[Name],
	                         Grouped.ModelId AS Id,
	                         M.[Name],
	                         I.[Name] As Icon,
	                         Grouped.Surplus,
	                         Grouped.Total,
                             P.[Days],
                             P.[Text],
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
                                     INNER JOIN [dbo].[Period] AS P
                                        ON P.Id = M.DefaultPeriod
                    ORDER BY Grouped.CollectionId, Asg.Due DESC;";

                var collectionsOfModels = new List<ListModelsByCollection<Model>>();
                var collectionDictionary = new Dictionary<int, int>();
                var modelDictionary = new Dictionary<int, List<Model>>();

                try
                {
                    db.Query<ListModelsByCollection<Model>, Model, Period, Asset, Asset>
                    (query, (collection, model, period, asset) =>
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
                        model.DefaultPeriod = period;
                        collectionsOfModels[collectionIndex].TList.Add(model);
                        collectionsOfModels[collectionIndex].Total += model.Total;
                        
                        //add a reference to each model to modelDictionary so we can append periodLists later
                        if (!modelDictionary.TryGetValue(model.Id, out List<Model> modelList))
                        {
                            modelList = new List<Model>();
                            modelDictionary.Add(model.Id, modelList);
                        }
                        modelList.Add(model);
                        return null;
                    }, new { userId = id }, splitOn: "Id, Id, Days, Id");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate model list.");
                }

                //fetch the list of periods for each unique model, and then append them to all instances of that
                //model found in collectionsOfModels. Periods are sorted with default first, and long-term last.
                query =
                    @"SELECT P.Id, P.[Days], P.[Text]
                        FROM [dbo].[Period] AS P
                             INNER JOIN [dbo].[PeriodList] AS PL
                                ON PL.PeriodId = P.Id
								   INNER JOIN [dbo].[Model] AS M
									  ON M.Id = PL.ModelId
                        WHERE PL.ModelId = @modelId
                        ORDER BY
						CASE WHEN P.[Days] = 0
						THEN 2 ELSE
							CASE WHEN P.Id = M.DefaultPeriod
							THEN 0 
							ELSE 1
						    END
						END, P.[Days]";

                foreach (KeyValuePair<int, List<Model>> pair in modelDictionary)
                {
                    var periods = new List<Period>();
                    try
                    {
                        periods.AddRange(db.Query<Period>(query, new { @modelId = pair.Key }));
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog(ex);
                        throw new LocusException("Unable to populate model period list.");
                    }
                    //we only want the period menu to show if there is more than one option, leave model.Period null
                    //will revert to the default period.
                    if (periods.Count > 1)
                    {
                        foreach (var model in pair.Value)
                        {
                            model.Periods = periods;
                        }
                    }
                }
                return collectionsOfModels;
            }
        }

        public IEnumerable<Role> GetAllRoles()
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string query = @"SELECT R.[Id],
                                        R.[Name]
                                   FROM [dbo].[Role] AS R
                                  WHERE R.Deactivated IS NULL;";
                try
                {
                    var result = db.Query<Role>(query);
                    if (result.Any())
                    {
                        return result;
                    }
                    throw new InvalidOperationException();
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to fetch list of User Roles.");
                }
            }
        }

        public UserDetails GetUserDetails(int id)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string query = @"SELECT U.[Name],
                                      U.Email,
                                      U.Phone,
                                      U.RoleId,
                                      U.Absentee,
                                      U.Comment
                                 FROM [dbo].[User] AS U
                                WHERE U.Id = @userId;";

                try
                {
                    return db.QuerySingle<UserDetails>(query, new { userId = id });
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to populate user details.");
                }
            }
        }

        public Tuple<IList<IResult>, int> CreateNewUser(UserCreatePostModel postModel)
        {
            var results = new List<IResult>();
            int userId;
            using (var scope = new TransactionScope())
            {
                var parameters = new DynamicParameters(postModel.UserDetails);
                using (var db = _connectionFactory.GetConnection())
                {
                    string query = @"INSERT INTO [dbo].[User]
                                   VALUES (@Name,
                                           @Absentee,
                                           @Email,
                                           @Phone,
                                           GETDATE(),
                                           @Comment,
                                           @RoleId)
                                    SELECT @UserId = SCOPE_IDENTITY()
							          FROM [dbo].[User]
								     WHERE Id = SCOPE_IDENTITY();";

                    parameters.Add("@UserId", 0, DbType.Int32, ParameterDirection.Output);
                    try
                    {
                        db.Execute(query, parameters);
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog(ex);
                        throw new LocusException("Unable to create new user.");
                    }
                    userId = parameters.Get<int>("@UserId");
                    foreach (var operation in postModel.AssignmentOperations)
                    {
                        results.Add(CreateNewAssignment(db, operation, userId));
                    }
                }
                if (results.OfType<PositiveResult>().Any())
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
            return new Tuple<IList<IResult>, int>(results, userId);
        }

        public IList<IResult> EditExistingUser(UserEditPostModel postModel)
        {
            var results = new List<IResult>();
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                string query = @"UPDATE [dbo].[User]
                                  SET [Name] = @Name,
                                      Absentee = @Absentee,
                                      Email = @Email,
	                                  Phone =	@Phone,
	                                  Comment = @Comment,
                                      RoleId = @RoleId
                                WHERE Id = @UserId;";

                var parameters = new DynamicParameters(postModel.UserDetails);
                parameters.Add("@UserId", postModel.UserId, DbType.Int32);
                try
                {
                    db.Execute(query, parameters);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("Unable to edit this User.");
                }

                if (postModel.AssignmentOperations != null)
                {
                    foreach (var operation in postModel.AssignmentOperations)
                    {
                        results.Add(CreateNewAssignment(db, operation, postModel.UserId));
                    }
                }
                if (postModel.EditOperations != null)
                {
                    //An SQL UPDATE will not throw an exception by default if it can't set.
                    //Most likely cause of this would be if the asset has already been returned.
                    //If @AssignmentId is null, the appended select query will return no rows
                    //and an Assignment object will be created with null members.
                    //hence we bypass this by manually throwing an exception if @AssignmentId is null.
                    foreach (var operation in postModel.EditOperations)
                    {
                        switch(operation.Type)
                        {
                            case OperationType.Return:
                                query = @"UPDATE [dbo].[Assignment]
                                           SET Returned = GETDATE(),
                                               @AssignmentId = Id
                                         WHERE AssetId = @AssetId
                                           AND Returned IS NULL
                                            IF (@AssignmentId IS NULL)
                                         THROW 50001,
                                               'AssignmentId is null',
                                               1;";
                                break;
                            case OperationType.Extension:

                                //assignments with period == 0 have a due field of NULL,
                                //which are treated as long-term assignments
                                //operation.period will be null if no period has been selected manually
                                string dueDate;
                                if (operation.Period > 0)
                                    dueDate = "DATEADD(DAY, " + operation.Period + @", GETDATE())";
                                else if (operation.Period == 0)
                                    dueDate = "NULL";
                                else
                                    dueDate = "DATEADD(DAY, P.[Days], GETDATE())";

                                query = @"UPDATE Asg
                                           SET Due = " + dueDate + @",
                                               @AssignmentId = Asg.Id
                                          FROM [dbo].[Assignment] AS Asg
	                                           INNER JOIN [dbo].[Asset] AS Ast
	                                              ON Ast.Id = Asg.AssetId
	                                                 INNER JOIN [dbo].[Model] AS M
		                                                ON M.Id = Ast.ModelId
                                                           INNER JOIN [dbo].[Period] AS P
                                                              ON P.Id = M.DefaultPeriod
                                         WHERE Asg.AssetId = @AssetId
                                           AND Asg.Returned IS NULL
                                            IF (@AssignmentId IS NULL)
                                         THROW 50001,
                                               'AssignmentId is null',
                                               1;";
                                break;
                        }
                        parameters = new DynamicParameters();
                        parameters.Add("@AssetId", operation.AssetId, DbType.String);
                        parameters.Add("@AssignmentId", -1, DbType.Int32, ParameterDirection.Output);
                        results.Add(DbQuery(operation, query, db, parameters));
                    }
                }              
            }
            return results;
        }

        //++++++++++++++++++
        //--HELPER METHODS--
        //++++++++++++++++++

        private IResult CreateNewAssignment(IDbConnection db, AssignmentOperation operation, int userId)
        {
            //assignments with period == 0 have a due field of NULL,
            //which are treated as long-term assignments
            //operation.Period will be null if no period has been selected manually
            string dueDate;
            if (operation.Period > 0)
            dueDate = "DATEADD(DAY, " + operation.Period + @", GETDATE())";
            else if (operation.Period == 0)
                dueDate = "NULL";
            else
                dueDate = "DATEADD(DAY, P.[Days], GETDATE())";

            string query = @"INSERT INTO [dbo].[Assignment]
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
                                   LEFT JOIN [dbo].[Period] AS P
							         ON P.Id = M.DefaultPeriod
                             WHERE M.Id = @ModelId
                            SELECT @AssignmentId = SCOPE_IDENTITY();";

            var parameters = new DynamicParameters(operation);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@AssignmentId", -1, DbType.Int32, ParameterDirection.Output);
            return DbQuery(operation, query, db, parameters);
        }

        private IResult DbQuery(Operation operation, string query, IDbConnection db, DynamicParameters parameters)
        {
            try
            {
                db.Execute(query, parameters);
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

        public Report GenerateReport(IList<IResult> results, int userId)
        {
            var listOfResultPartials = new List<IPartialReportItem<IReportItem>>();
            var listOfPrintablePartials = new List<IPartialReportItem<DetailedReportItem>>();
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                try
                {
                    string query = "";
                    query = @"SELECT C.Id,
	                            C.[Name],
	                            M.[Name] AS Model,
	                            I.[Name] AS Icon,
	                            Ast.Tag,
                                Asg.Id AS AssignmentId,
                                M.Id AS ModelId,
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
                            WHERE Asg.UserId = @UserId
                            AND Asg.Returned IS NULL";

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", userId, DbType.Int32);
                    listOfPrintablePartials.AddRange(CreatePartialReportItem<DetailedReportItem>(OperationType.None, query, db, parameters));
                    if (results != null)
                    {
                        foreach (var result in results)
                        {
                            parameters = new DynamicParameters(result);
                            if (result is PositiveResult)
                            {
                                if (result.Type != OperationType.Return)
                                {
                                    //positive results that are not of type.return produce detailedReportItems.
                                    //Since a partialReportItem will already have been created for this asset in
                                    //listOfPrintablePartials, we can copy a reference so as not to double up on
                                    //appending custom properties etc.
                                    var positiveResult = result as PositiveResult;
                                    var partialReference = listOfPrintablePartials.Find(x => x.ReportItem.AssignmentId == positiveResult.AssignmentId);
                                    partialReference.ReportItem.Type = result.Type;
                                    listOfResultPartials.Add(partialReference);
                                }
                                else
                                {
                                    listOfResultPartials.AddRange(CreatePartialReportItem<SimpleReportItem>(result.Type, result.QueryString, db, parameters));
                                }
                            }
                            else
                            {
                                listOfResultPartials.AddRange(CreatePartialReportItem<ErrorReportItem>(result.Type, result.QueryString, db, parameters));
                            }
                        }
                    }
                    var printableCollections = new List<ListTByCollection<DetailedReportItem>>();
                    var printableCollectionDictionary = new Dictionary<int, int>();
                    var resultCollections = new List<ListTByCollection<IReportItem>>();
                    var resultCollectionDictionary = new Dictionary<int, int>();
                    if (listOfPrintablePartials.Any())
                    {
                        foreach (var partial in listOfPrintablePartials)
                        {
                            AppendCustomeProperties(partial, db);
                            CommitItemToReport(partial, printableCollectionDictionary, printableCollections);
                        }
                    }
                    if (listOfResultPartials.Any())
                    {
                        foreach (var partial in listOfResultPartials)
                        {
                            CommitItemToReport(partial, resultCollectionDictionary, resultCollections);
                        }
                    }

                    query = @"SELECT [Name] AS UserName,
                               Created AS UserCreated
                          FROM [dbo].[User]
                         WHERE Id = @UserId;";

                    var report = db.QuerySingle<Report>(query, new { userId });
                    report.UserId = userId;
                    report.UserStatus = UserStatus(userId, db);
                    report.ResultCollections = resultCollections;
                    report.PrintableCollections = printableCollections;
                    return report;
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                    throw new LocusException("An error occured while attempting to produce a report.");
                }
            }
        }

        private IEnumerable<IPartialReportItem<T>> CreatePartialReportItem<T>(OperationType type, string query, IDbConnection db, DynamicParameters parameters)
            where T : SimpleReportItem
        {
            var item = db.Query<Collection, T, IPartialReportItem<T>>
            (query, (collection, reportItem) =>
            {
                reportItem.Type = type;
                var newItem = new PartialReportItem<T>
                {
                    Collection = collection,
                    ReportItem = reportItem
                };
                return newItem;
            }, parameters, splitOn: "Model");
            return item;
        }

        private void AppendCustomeProperties(IPartialReportItem<DetailedReportItem> item, IDbConnection db)
        {
            string query = @"SELECT [Name],
                                  QueryString AS [Value]
                             FROM [dbo].[Query]
                            WHERE ModelId = @ModelId";

            //fetch all the query strings that relate to this model
            var parameters = new DynamicParameters();
            parameters.Add("@ModelId", item.ReportItem.ModelId, DbType.Int32);
            try
            {
                var queries = db.Query<CustomProperty>(query, parameters);
                if (queries.Any())
                {
                    query = @"SELECT Asg.Id AS AssignmentId,
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

                    //create dynamic parameters for all potential values that a query may need
                    parameters.Add("@AssignmentId", item.ReportItem.AssignmentId, DbType.Int32);
                    parameters = new DynamicParameters(db.Query(query, parameters).Cast<IDictionary<string, object>>().ElementAt(0));
                    item.ReportItem.CustomProperties = new List<CustomProperty>();
                    foreach (var sqlQuery in queries)
                    {
                        var property = new CustomProperty
                        {
                            Name = sqlQuery.Name,
                            Value = db.ExecuteScalar<string>(sqlQuery.Value, parameters)
                        };
                        item.ReportItem.CustomProperties.Add(property);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }     

        private void CommitItemToReport<T>(IPartialReportItem<T> item, Dictionary<int, int> dictionary, List<ListTByCollection<T>> collections)
            where T : IReportItem
        {
            if (!dictionary.TryGetValue(item.Collection.Id, out int collectionIndex))
            {
                collectionIndex = collections.Count();
                dictionary.Add(item.Collection.Id, collectionIndex);
                var collection = new ListTByCollection<T>
                {
                    Id = item.Collection.Id,
                    Name = item.Collection.Name,
                    TList = new List<T>()
                };
                collections.Add(collection);
            }
            collections[collectionIndex].TList.Add(item.ReportItem);
        }

        public Status UserStatus(int userId, IDbConnection db)
        {
            if (db == null)
                db = _connectionFactory.GetConnection();

            using (db)
            {
                string query = @"SELECT COUNT(*)
					         FROM [dbo].[Assignment]
						    WHERE UserId = @UserId
						      AND Returned IS NULL;";

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId, DbType.Int32);
                try
                {
                    int assetCount = db.ExecuteScalar<int>(query, parameters);
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
            return Status.Long_Term;
        }

        private int GetScalar(string query)
        {
            using (IDbConnection db = _connectionFactory.GetConnection())
            {
                try
                {
                    return db.ExecuteScalar<int>(query);
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