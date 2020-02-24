using System.Collections.Generic;

namespace Locus.Models
{
    public class GroupTByDivision<T> : Division
    {
        public IList<T> ListOfGeneric { get; set; }
    }
}
