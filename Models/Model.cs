using System.Collections.Generic;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public int Surplus { get; set; }
        public int Total { get; set; }
        public Period DefaultPeriod { get; set; }
        public IEnumerable<Period> Periods { get; set; }
        public Asset Asset { get; set; }
    }
}