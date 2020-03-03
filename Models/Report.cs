using System;
using System.Collections.Generic;

namespace Locus.Models
{
    public class Report
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public DateTime UserCreated { get; set; }
        public Status UserStatus { get; set; }
        public IEnumerable<ListTByCollection<IReportItem>> CollectionsOfReportItems { get; set; }
    }
}