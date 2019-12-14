using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class Icon
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
