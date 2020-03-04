namespace Locus.Models
{
    public class NegativeEditResult : Result
    {
        public string AssetId { get; set; }
        private readonly static string _query =
            @"SELECT C.Id,
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
        public override string QueryString { get { return _query; } }

        public NegativeEditResult(EditOperation operation)
        {
            AssetId = operation.AssetId;
            Type = operation.Type;
        }
    }
}
