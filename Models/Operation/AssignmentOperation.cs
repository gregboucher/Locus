using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class AssignmentOperation
    {
        [Required]
        public int ModelId { get; set; }
        [Required]
        public int CollectionId { get; set; }
        [Required]
        public OperationType Type { get; set; }
    }
}