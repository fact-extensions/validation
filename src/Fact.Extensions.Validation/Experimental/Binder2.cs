using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Fact.Extensions.Validation.Experimental
{
    /// <summary>
    /// Boilerplate for less-typed filter-only style binder
    /// </summary>
    public class Binder2 : BinderBase
    {
        public Binder2(IField field) : base(field)
        {
        }
    }
}