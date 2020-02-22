using System.Collections.Generic;

namespace Locus.Models
{
    public class GroupedUsers : Group
    {
        public IList<User> Users { get; set; }
    }

}
