using System;

namespace Locus.Models
{
    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? Deactivated { get; set; }
    }
}