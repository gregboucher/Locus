namespace Locus.Models
{
    public class SimpleReportItem : IReportItem
    {
        public string Model { get; set; }
        public string Icon { get; set; }
        public string Tag { get; set; }
        public OperationType Type { get; set; }
    }
}
