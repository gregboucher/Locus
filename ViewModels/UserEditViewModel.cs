using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class UserEditViewModel : BaseViewModel
    {
        public int UserId { get; set; }
        public UserDetails UserDetails { get; set; }
        public IEnumerable<Role> Roles { get; set; }
        public IEnumerable<Period> Periods { get; set; }
        public IEnumerable<ListModelsByCollection<Model>> CollectionsOfModels { get; set; }
    }
}