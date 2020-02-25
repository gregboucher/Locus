using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class EditOperation
    {
        [Required]
        public string AssetId { get; set; }
        [Required]
        public OperationType Type { get; set; }
    }
}