using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class HomeDashboardViewModel
    {
        public int DueTodayCount { get; set; }
        public int OverdueCount { get; set; }
        public int CreatedTodayCount { get; set; }
        public IEnumerable<Group_User> Groups { get; set; }
    }
}

