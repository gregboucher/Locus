using System;
using System.Collections.Generic;

namespace Locus.Models
{
    public class DetailedSummary : Summary
    {
        public int ModelId { get; set; }
        public DateTime Due { get; set; }
        public IList<CustomProperty> CustomProperties { get; set; }
    }
}