using System;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Tag, nq}")]
    public class Asset
    {
        public string Tag { get; set; }
        public string Icon { get; set; }
        public DateTime Due { get; set; }
        public string Status { get; set; }
    }
}