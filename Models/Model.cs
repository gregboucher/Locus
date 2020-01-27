using System;
using System.Diagnostics;

namespace Locus.Models
{
    [DebuggerDisplay("{Name, nq}")]
    public class Model
    {
        public Icon Icon { get; set; }
    }
}