using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class HomeDashboardViewModel : BaseViewModel
    {
        public int CountDueToday { get; set; }
        public int CountOverdue { get; set; }
        public int CountCreatedToday { get; set; }
        public IEnumerable<GroupedUsers> GroupedUsers { get; set; }
    }
}