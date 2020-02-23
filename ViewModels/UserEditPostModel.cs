using Locus.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Locus.ViewModels
{
    public class UserEditPostModel
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public UserDetails UserDetails { get; set; }
        public IEnumerable<AssignSelection> AssignSelections { get; set; }
        public IList<EditSelection> EditSelections { get; set; }
    }
}