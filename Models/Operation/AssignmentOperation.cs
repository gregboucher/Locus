using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class AssignmentOperation : Operation
    {
        [Required]
        public int ModelId { get; set; }
        [Required]
        public int CollectionId { get; set; }
    }
}