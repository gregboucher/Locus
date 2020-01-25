using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Locus.Models
{
    public class AssetList
    {
        public IList<string> Returns { get; set; }
        public IList<string> Extends { get; set; }
        public IList<string> Assigns { get; set; }
    }
}
