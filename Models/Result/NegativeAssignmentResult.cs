namespace Locus.Models
{
    public class NegativeAssignmentResult : Result
    {
        public int ModelId { get; set; }
        public int CollectionId { get; set; }

        public NegativeAssignmentResult(AssignmentOperation operation)
        {
            ModelId = operation.ModelId;
            CollectionId = operation.CollectionId;
            Type = operation.Type;
        }
    }
}