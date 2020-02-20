using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class UserSummaryViewModel : BaseViewModel
    {
        public IEnumerable<GroupOfAssignments> Groups { get; set; }
    }
}
