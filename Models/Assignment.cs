using System;

namespace Locus.Models
{
    public class Assignment
    {
        public string Model { get; set; }
        public string Icon { get; set; }
        public string Tag { get; set; }
    }

    public class ErrorAssignment : Assignment
    {
        public string Message { get; set; }
    }

    public class ReturnedAssignment : Assignment
    {
    }

    public class QualifiedAssignment : Assignment
    {
        public ActionType Action { get; set; }
        public DateTime Due { get; set; }
        public string Password { get; set; }
    }
}