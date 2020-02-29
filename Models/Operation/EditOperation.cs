﻿using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class EditOperation : Operation
    {
        [Required]
        public string AssetId { get; set; }
    }
}