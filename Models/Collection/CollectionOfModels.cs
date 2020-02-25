namespace Locus.Models
{ 
    public class CollectionOfModels<T> : CollectionOfGenerics<T>
    {
        public int Total { get; set; }
        public int TotalAssigned { get; set; }
    }
}
