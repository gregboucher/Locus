using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class CurrentAssignment
    {
        [Required]
        public string AssetId { get; set; }
        [Required]
        public string Action { get; set; }
    }
}
