using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? Deactivated { get; set; }

        public IList<User> Users { get; set; }
    }
}