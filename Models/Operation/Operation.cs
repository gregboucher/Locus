using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public abstract class Operation
    {
        [Required]
        public OperationType Type { get; set; }
        public int Period { get; set; }
    }
}
