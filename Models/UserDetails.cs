using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class UserDetails
    {
        [Required]
        [StringLength(64)]
        public string Name { get; set; }
        [DataType(DataType.EmailAddress)]
        [StringLength(256, MinimumLength = 7)]
        public string Email { get; set; }
        [DataType(DataType.PhoneNumber)]
        [StringLength(16)]
        public string Phone { get; set; }
        [Required]
        public int RoleId { get; set; }
        [StringLength(64)]
        public string Absentee { get; set; }
        [StringLength(256)]
        public string Comment { get; set; }
    }
}
