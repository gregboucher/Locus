﻿using Locus.Models;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class HomeDashboardViewModel
    {
        public int CountDueToday { get; set; }
        public int CountOverdue { get; set; }
        public int CountCreatedToday { get; set; }
        public IEnumerable<GroupOfUsers> Groups { get; set; }
        public string Icon { get; set; }
    }
}

