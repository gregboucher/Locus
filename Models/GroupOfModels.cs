using System.Diagnostics;
using System.Collections.Generic;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class GroupOfModels
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Total { get; set; }
        public int TotalAssigned { get; set; }
        public IList<Model> Models { get; set; }
    }
}
