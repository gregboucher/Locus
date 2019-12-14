using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class HomeDashboardViewModel
    {
        public string Controller { get; set; }
        public string Action { get; set; }
        public int AssignedUserCount { get; set; }
        public int DueTodayCount { get; set; }
        public int OverdueCount { get; set; }
        public IEnumerable<Group> Groups { get; set; }
    }

}

