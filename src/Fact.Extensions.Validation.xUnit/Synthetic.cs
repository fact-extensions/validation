using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;

    public class SyntheticEntity1
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password1 { get; set; }
        [Required]
        public string Password2 { get; set; }
    }
}
