namespace Locus.Models
{ 
    public class ListModelsByCollection<T> : ListTByCollection<T>
    {
        public int Total { get; set; }
        public int TotalAssigned { get; set; }
    }
}