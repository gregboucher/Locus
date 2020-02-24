namespace Locus.Models
{
    public class PendingSummary<T> where T : Summary
    {
        public Group Group { get; set; }
        public T Assignment { get; set; }
    }
}