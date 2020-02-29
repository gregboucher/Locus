using System.Collections.Generic;

namespace Locus.Models
{
    public class ListTByCollection<T> : Collection
    {
        public IList<T> TList { get; set; }
    }
}