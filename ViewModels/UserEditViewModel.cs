using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class UserEditViewModel : BaseViewModel
    {
        public UserDetails UserDetails { get; set; }
        public IEnumerable<Role> Roles { get; set; }
        public IEnumerable<GroupOfModels> Groups { get; set; }
    }
}
