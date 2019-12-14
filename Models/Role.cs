using System;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? Deactivated { get; set; }
    }
}