namespace Locus.Models
{
    public interface IResult
    {
        public OperationType Type { get; set; }
        public string QueryString { get; }
        public IReportItem ReportItemType { get; }
    }
}
