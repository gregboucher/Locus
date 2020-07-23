using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class UserCreateViewModel : BaseViewModel
    {
        public IEnumerable<Role> Roles { get; set; }
        public IEnumerable<Period> Periods { get; set; }
        public IEnumerable<ListModelsByCollection<Model>> CollectionsOfModels { get; set; }
    }
}
