using System;
using System.Collections.Generic;

namespace Locus.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? Deactivated { get; set; }
    }
}