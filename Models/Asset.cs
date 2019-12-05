using System;

namespace Locus.Models
{
    public class Asset
    {
        public string Id { get; set; }
        public string Tag { get; set; }
        public Model Model { get; set; }
        public Group Group { get; set; }
        public DateTime? Deactivated { get; set; }
    }
}