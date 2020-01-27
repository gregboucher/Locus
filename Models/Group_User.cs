using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class Group_User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<User> Users { get; set; }
    }
}