using System;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Tag, nq}")]
    public class Asset
    {
        public string Id { get; set; }
        public string Tag { get; set; }
        public string Icon { get; set; }
        public string Model { get; set; }
        public DateTime Assigned { get; set; }
        public DateTime? Due { get; set; }
        public Status Status { get; set; }
    }
}