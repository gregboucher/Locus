using Locus.Models;
using System;
using System.Collections.Generic;

namespace Locus.ViewModels
{
    public class UserSummaryViewModel : BaseViewModel
    {
        public UserSummary User { get; set; }
    }

    public class UserSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public Status Status { get; set; }
        public IEnumerable<CollectionOfGenerics<ReportItem>> CollectionsOfReportItems { get; set; }
    }
}