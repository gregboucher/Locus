﻿using System.ComponentModel.DataAnnotations;

namespace Locus.Models
{
    public class EditSelection : ISelection
    {
        [Required]
        public string AssetId { get; set; }
        [Required]
        public SelectionType Type { get; set; }
    }
}