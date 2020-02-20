using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class EditAssignment
    {
        [Required]
        public string AssetId { get; set; }
        [Required]
        public string Action { get; set; }
    }
}
