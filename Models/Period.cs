using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Days, nq}")]
    public class Period
    {
        public int Days { get; set; }
        public string Text { get; set; }
    }
}