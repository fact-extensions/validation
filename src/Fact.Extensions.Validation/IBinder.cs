using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    public interface IBinderBase
    {
        /// <summary>
        /// Original 'canonical' field with aggregated/total status
        /// </summary>
        IField Field { get; }

        Func<object> getter { get; }

        Experimental.Committer Committer { get; }
    }



}
