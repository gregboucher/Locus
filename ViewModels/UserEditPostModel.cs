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
        public IEnumerable<AssignmentOperation> AssignmentOperations { get; set; }
        public IList<EditOperation> EditOperations { get; set; }
    }
}