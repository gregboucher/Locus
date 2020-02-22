using System;

namespace Locus.Models
{
    public class QualifiedAssignment : Assignment
    {
        public ActionType Action { get; set; }
        public DateTime Due { get; set; }
        public string Password { get; set; }
    }
}
