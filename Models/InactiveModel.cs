using System;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Model, nq}")]
    public class InactiveModel
    {
        public int Id { get; set; }
        public string Model { get; set; }
        public string Icon { get; set; }
        public int Period { get; set; }
        public int Surplus { get; set; }
        public int Total { get; set; }
    }
}
