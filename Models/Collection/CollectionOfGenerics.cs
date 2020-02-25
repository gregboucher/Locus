using System.Collections.Generic;

namespace Locus.Models
{
    public class CollectionOfGenerics<T> : Collection
    {
        public IList<T> TList { get; set; }
    }
}