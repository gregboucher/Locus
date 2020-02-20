using System.Collections.Generic;

namespace Locus.Models
{
    public class GroupOfAssignments
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<Assignment> Assignments { get; set; }
    }
}
