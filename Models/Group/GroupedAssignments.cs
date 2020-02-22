using System.Collections.Generic;

namespace Locus.Models
{
    public class GroupedAssignments : Group
    {
        public IList<Assignment> Assignments { get; set; }
    }
}
