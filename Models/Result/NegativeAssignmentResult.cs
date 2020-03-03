namespace Locus.Models
{
    public class NegativeAssignmentResult : Result
    {
        public int ModelId { get; set; }
        public int CollectionId { get; set; }
        public override IReportItem ReportItemType { get { return new ErrorReportItem(); } }
        private readonly static string _query =
            @"SELECT C.Id,
                     C.[Name],
                     M.[Name] AS Model,
                     I.[Name] AS Icon
                FROM [dbo].[Model] AS M
                     INNER JOIN [dbo].[Icon] AS I
                        ON I.Id = M.IconId
                     CROSS JOIN [dbo].[Collection] AS C
               WHERE M.Id = @ModelId
                 AND C.Id = @CollectionId;";
        public override string QueryString { get { return _query; }}

        public NegativeAssignmentResult(AssignmentOperation operation)
        {
            ModelId = operation.ModelId;
            CollectionId = operation.CollectionId;
            Type = operation.Type;
        }
    }
}