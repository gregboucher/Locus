using System.Collections.Generic;

namespace Locus.Models
{
    public class GroupedModels : Group
    {
        public int Total { get; set; }
        public int TotalAssigned { get; set; }
        public IList<Model> Models { get; set; }
    }
}
