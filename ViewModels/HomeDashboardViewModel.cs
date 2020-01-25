using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class HomeDashboardViewModel
    {
        public int DistinctUsersByGroupCount { get; set; }
        public int DueTodayCount { get; set; }
        public int OverdueCount { get; set; }
        public int CreatedTodayCount { get; set; }
        public IEnumerable<Group> Groups { get; set; }
    }
}

