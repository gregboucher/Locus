using System;

namespace Locus.Models
{
    public class CompletedAssignment : Assignment
    {
        public string Action { get; set; }
        public DateTime Due { get; set; }
        public string Password { get; set; }
    }
}
