using System;
using System.Collections.Generic;

namespace Locus.Models
{
    public class DetailedReportItem : ReportItem
    {
        public int AssignmentId { get; set; }
        public int ModelId { get; set; }
        public DateTime Due { get; set; }
        public IList<CustomProperty> CustomProperties { get; set; }
    }
}