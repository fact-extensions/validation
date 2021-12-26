using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation.Synthetic
{
    using Experimental;
    
    public interface IAddressCountry
    {
        string Country { get; }
    }

    public class UsAddress : IAddressCountry
    {
        public string Country => "US";

        [Required]
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        
        public string Zip { get; set; }
    
        [Required]
        public string City { get; set; }
    }
}
