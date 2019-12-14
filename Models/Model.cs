using System;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Period { get; set; }
        public Icon Icon { get; set; }
        public DateTime? Deactivated { get; set; }
    }
}