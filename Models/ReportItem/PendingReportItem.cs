namespace Locus.Models
{
    public class PendingReportItem<T> where T : ReportItem
    {
        public Collection Collection { get; set; }
        public T ReportItem { get; set; }
    }
}