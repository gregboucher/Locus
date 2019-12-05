using System;

namespace Locus.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? Deactivated { get; set; }
    }
}