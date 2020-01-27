using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Locus.Models
{
    public class Group_InactiveModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Total { get; set; }
        public IList<InactiveModel> InactiveModels { get; set; }
    }
}
