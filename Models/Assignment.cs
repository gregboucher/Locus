using System;

namespace Locus.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        public User User { get; set; }
        public Asset Asset { get; set; }
        public DateTime? Returned { get; set; }
    }
}