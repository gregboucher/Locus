using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class Icon
    {
        public string Name { get; set; }
    }
}