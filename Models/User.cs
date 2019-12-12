using System;
using System.Collections.Generic;

namespace Locus.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Absentee { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime Created { get; set; }
        public Role Role { get; set; }

        public IList<Asset> Assets { get; set; }
    }
}