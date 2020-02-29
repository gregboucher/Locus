namespace Locus.Models
{
    public class PositiveResult : Result
    {
        public int AssignmentId { get; set; }

        public PositiveResult(int assignmentId, Operation operation)
        {
            AssignmentId = assignmentId;
            Type = operation.Type;
        }
    }
}
