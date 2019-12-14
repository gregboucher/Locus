using System;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Id, nq}")]
    public class Assignment
    {
        public int Id { get; set; }
        public DateTime Assigned { get; set; }
        public DateTime Due { get; set; }
        public DateTime? Returned { get; set; }
        public User User { get; set; }
        public Asset Asset { get; set; }
    }
}