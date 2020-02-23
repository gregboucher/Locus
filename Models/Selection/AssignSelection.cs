using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class AssignSelection : ISelection
    {
        [Required]
        public int ModelId { get; set; }
        [Required]
        public int GroupId { get; set; }
    }
}
