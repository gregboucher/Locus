namespace Locus.Models
{
    public abstract class Result : IResult
    {
        public OperationType Type { get; set; }
        public abstract string QueryString { get; }
    }
}
