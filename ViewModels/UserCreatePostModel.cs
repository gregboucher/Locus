using Locus.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Locus.ViewModels
{
    public class UserCreatePostModel
    {
        [Required]
        public UserDetails UserDetails { get; set; }
        [Required]
        public IEnumerable<AssignmentOperation> AssignmentOperations { get; set; }
    }
}