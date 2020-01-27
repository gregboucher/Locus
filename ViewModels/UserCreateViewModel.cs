using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class UserCreateViewModel
    {
        public IEnumerable<Role> Roles { get; set; }
        public IEnumerable<Group_InactiveModel> Groups { get; set; }
    }
}
