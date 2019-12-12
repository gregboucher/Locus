using System;

namespace Locus.Models
{
    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Period { get; set; }
        public Icon Icon { get; set; }
        public DateTime? Deactivated { get; set; }
    }
}