using System.Diagnostics;
using System.Collections.Generic;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class GroupOfUsers
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<User> Users { get; set; }
    }
}