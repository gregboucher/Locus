namespace Locus.Models
{
    public class PartialReportItem<T> : IPartialReportItem<T> where T : IReportItem
    {
        public Collection Collection { get; set; }
        public T ReportItem { get; set; }
    }
}