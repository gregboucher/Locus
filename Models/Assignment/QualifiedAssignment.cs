using System;

namespace Locus.Models
{
    public class QualifiedAssignment : Assignment
    {
        public DateTime Due { get; set; }
        public string Password { get; set; }
    }
}
