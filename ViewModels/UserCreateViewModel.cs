using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class UserCreateViewModel : BaseViewModel
    {
        public IEnumerable<Role> Roles { get; set; }
        public IEnumerable<GroupOfModels> Groups { get; set; }
    }
}
