using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public string Role { get; set; }
        public IList<Asset> Assets { get; set; }
    }
}