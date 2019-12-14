using System;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Tag, nq}")]
    public class Asset
    {
        public string Id { get; set; }
        public string Tag { get; set; }
        public Model Model { get; set; }
        public Group Group { get; set; }
        public DateTime Due { get; set; }
        public string Status { get; set; }
        public DateTime? Deactivated { get; set; }
    }
}