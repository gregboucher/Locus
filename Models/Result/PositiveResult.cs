namespace Locus.Models
{
    public class PositiveResult : Result
    {
        public int AssignmentId { get; set; }
        private readonly static string _query =
            @"SELECT C.Id,
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
        public override string QueryString { get { return _query; } }

        public PositiveResult(int assignmentId, Operation operation)
        {
            AssignmentId = assignmentId;
            Type = operation.Type;
        }
    }
}