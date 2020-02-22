using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class NewSelection : ISelection
    {
        [Required]
        public int ModelId { get; set; }
        [Required]
        public int GroupId { get; set; }
    }
}
