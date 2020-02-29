namespace Locus.Models
{
    public interface IPartialReportItem<out T> where T : IReportItem
    {
        public Collection Collection { get; set; }
        public T ReportItem { get; }
    }
}