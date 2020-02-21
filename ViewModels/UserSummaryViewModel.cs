using Locus.Models;
using System;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class UserSummaryViewModel : BaseViewModel
    {
        public QualifiedUser User { get; set; }
    }

    public class QualifiedUser
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public IEnumerable<GroupOfAssignments> GroupedAssignments { get; set; }
    }
}
